"""Merges the coverage reports of every test project and enforces a line-coverage minimum per assembly.

Each test project writes its own Cobertura report (dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"), and each only
knows the lines its own tests ran. A line counts as covered here if any project's tests ran it.

    python scripts/check_coverage.py <results-directory> QualiTrack.Domain=95 QualiTrack.Application=85

Prints a Markdown table (to the GitHub job summary too, when run in Actions) and exits 1 if any assembly is
below its minimum.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ElementTree


def merged_line_hits(results_directory):
    """Returns {assembly: {(class, line): was_hit}} across every report under the directory."""
    assemblies = {}
    reports = glob.glob(os.path.join(results_directory, "**", "coverage.cobertura.xml"), recursive=True)
    if not reports:
        sys.exit(f"No coverage.cobertura.xml found under {results_directory}")

    for report in reports:
        for package in ElementTree.parse(report).getroot().iter("package"):
            lines = assemblies.setdefault(package.get("name"), {})
            for source_class in package.iter("class"):
                for line in source_class.iter("line"):
                    # Keyed by class, not file: each report writes file paths relative to a different folder
                    key = (source_class.get("name"), int(line.get("number")))
                    lines[key] = lines.get(key, False) or int(line.get("hits")) > 0
    return assemblies


def main(arguments):
    if len(arguments) < 2:
        sys.exit(__doc__)

    assemblies = merged_line_hits(arguments[0])
    minimums = {name: float(minimum) for name, minimum in (pair.split("=") for pair in arguments[1:])}

    rows = ["| Assembly | Line coverage | Minimum | |", "| --- | ---: | ---: | --- |"]
    failed = False
    for name in sorted(assemblies):
        lines = assemblies[name]
        percent = 100.0 * sum(lines.values()) / len(lines) if lines else 100.0
        minimum = minimums.get(name)
        passed = minimum is None or percent >= minimum
        failed = failed or not passed
        rows.append(
            f"| {name} | {percent:.1f} % | {'' if minimum is None else f'{minimum:.0f} %'} | "
            f"{'' if minimum is None else ('ok' if passed else 'BELOW MINIMUM')} |"
        )

    for name in sorted(set(minimums) - set(assemblies)):
        failed = True
        rows.append(f"| {name} | not measured | {minimums[name]:.0f} % | BELOW MINIMUM |")

    table = "\n".join(rows)
    print(table)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as handle:
            handle.write("## Coverage\n\n" + table + "\n")

    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main(sys.argv[1:])
