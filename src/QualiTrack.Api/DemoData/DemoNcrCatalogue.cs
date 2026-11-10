using QualiTrack.Domain.NonConformances;

namespace QualiTrack.Api.DemoData;

/// <summary>
/// The demo's NCRs, oldest first: one was raised each day, the last of them today. Older ones have mostly run
/// their course; the unfinished ones among them are what the overdue badge and the dashboard have to show.
/// </summary>
internal static class DemoNcrCatalogue
{
    public static IReadOnlyList<DemoNcr> All { get; } =
    [
        Closed(Severity.Major, "Weld porosity on frame bracket FB-220", "Visible porosity along 60 mm of the fillet weld on three brackets from the morning shift.", "Welding bay 2",
            "Shielding gas flow had dropped below 12 l/min after a regulator change.", "Grind out and re-weld the three brackets", "Add a gas flow check to the shift start checklist"),
        Closed(Severity.Minor, "Batch label misprinted on cartons", "Batch code printed as 2B instead of 28 on 40 cartons of fastener kits.", "Packing line 1",
            "The printer template was edited by hand instead of loaded from the order.", "Relabel the 40 cartons", "Lock the template so the batch code comes from the order"),
        Closed(Severity.Critical, "Torque wrench out of calibration on Line 2", "Wrench TW-17 read 8% low at the weekly check; it was used on wheel hub bolts since Monday.", "Assembly line 2",
            "Calibration was due last month; the recall list missed tools added this year.", "Re-torque every hub assembled since Monday", "Add all tools bought this year to the calibration recall list"),
        Cancelled(Severity.Minor, "Scratch on side panel SP-4", "Light scratch about 30 mm long near the lower edge of one panel.", "Paint shop exit",
            "Raised twice: the same panel is already reported in the previous NCR."),
        Closed(Severity.Major, "Bore oversize on gearbox housing H-14", "Bore measured 52.08 mm against 52.00 +0.03; five housings from one setup.", "CNC cell 3",
            "Boring bar insert was worn past its tool life count.", "Scrap the five housings and re-run the batch", "Set the tool life counter to stop the cycle at the limit"),
        Closed(Severity.Minor, "Paint run on door panel", "Run about 80 mm long below the handle recess on two doors.", "Paint booth 1",
            "Spray gun fan pattern was set too narrow after cleaning.", "Sand and respray the two doors", "Post the gun settings card at the booth"),
        InProgress(Severity.Major, "Coolant contamination in CNC cell 4", "Tramp oil layer and sour smell in the sump; surface finish on aluminium parts has degraded.", "CNC cell 4",
            "The oil skimmer belt had snapped and nobody was assigned to check it.", "Drain, clean and refill the sump", "Add the skimmer to the weekly maintenance round"),
        Closed(Severity.Major, "Wrong gasket material issued to assembly", "Cork gaskets issued where the drawing calls for nitrile; found on the first-off check.", "Stores, aisle C",
            "Both gaskets share a bin location after the stores re-layout.", "Recover the cork gaskets from the line and issue nitrile", "Give each gasket its own labelled bin"),
        Closed(Severity.Critical, "No interlock test record for press guard", "The monthly interlock test on press P-6 has no record for the last two months.", "Press shop",
            "The test was done but the form lived on a clipboard that was lost.", "Test the interlock today and record the result", "Move the interlock record into the maintenance system"),
        Pending(Severity.Minor, "Foam insert undersized in export crates", "Insert leaves a 15 mm gap; the pump can move in transit.", "Packing line 2",
            "The supplier cut to an old drawing revision.", "Add a foam shim to the crates in stock", "Send the current drawing and ask the supplier to confirm it"),
        Closed(Severity.Major, "Thread damage on M12 studs from supplier", "About one in ten studs in lot 4471 fails the go gauge.", "Goods inwards",
            "Studs were packed loose and knocked against each other in transit.", "Sort lot 4471 with the go gauge and return rejects", "Agree layered packing with the supplier"),
        Investigating(Severity.Minor, "Faded batch code on drum labels", "Batch codes on six drums of cutting fluid are too faint to read under store lighting.", "Chemical store"),
        Closed(Severity.Minor, "Burr on stamped bracket edge", "Sharp burr on the pierced hole of bracket BR-9; a glove was cut at assembly.", "Press shop",
            "Punch clearance had opened up; the die was overdue for a regrind.", "Deburr the batch by hand", "Regrind the die and log the stroke count"),
        Cancelled(Severity.Major, "Suspected hardness failure on shaft batch", "First reading on two shafts was 4 HRC below the minimum.", "Heat treatment lab",
            "The tester's indenter was chipped; the retest on a second machine passed, so nothing is non-conforming."),
        Closed(Severity.Major, "Conveyor misalignment crushing cartons", "Side guide on conveyor C-2 pinches cartons at the transfer; eleven damaged this week.", "Dispatch",
            "A guide bracket bolt had worked loose.", "Re-align the guide and replace the bolt with a locking one", "Repack the eleven damaged cartons"),
        Rework(Severity.Major, "Leak at hydraulic fitting after pressure test", "Weep at the elbow fitting on manifold M-3 during the 10-minute hold.", "Test bay",
            "The fitting was tightened by feel, without a torque value on the job card.", "Re-seat and torque the fitting, then re-test", "Add torque values to the manifold job card",
            "The re-test record shows a 5-minute hold; the procedure requires 10. Please repeat it for the full time."),
        Closed(Severity.Critical, "Wrong heat treatment cycle recorded", "Furnace chart for load 318 shows 820 °C; the route card calls for 860 °C.", "Heat treatment",
            "The operator selected the neighbouring recipe on the furnace panel.", "Re-treat load 318 and hardness test every part", "Ask for the recipe number to be scanned from the route card"),
        Closed(Severity.Minor, "Washer missing from fastener kits", "Three kits out of fifty sampled have five washers instead of six.", "Kitting bench",
            "Kits are counted by hand at the end of a shift.", "Check and top up the affected kits", "Weigh every kit against a target weight"),
        InProgress(Severity.Minor, "Surface rust on stored steel coil", "Light rust on the outer wraps of coil 22-118 after six weeks in store.", "Raw material store",
            "The coil was stored next to the loading door without its wrap.", "Clean the outer wraps and re-wrap the coil", "Mark a no-storage zone by the loading door"),
        Closed(Severity.Major, "Dimension drift on turned shaft S-08", "Journal diameter crept from mid-tolerance to the top limit across a run of 200.", "CNC cell 1",
            "No in-process measurement between first-off and last-off.", "Measure the run and segregate anything out of tolerance", "Add a check every 25 parts to the control plan"),
        Pending(Severity.Major, "Gaps in adhesive bead on cab glazing", "Bead has two gaps of about 20 mm at the lower corners on four cabs.", "Glazing station",
            "The nozzle was partly blocked with cured adhesive.", "Re-bond the four cabs", "Replace the nozzle at each shift change"),
        Closed(Severity.Minor, "Old drawing revision at workstation", "Revision C of drawing D-1042 found at the bench; revision E is current.", "Assembly line 1",
            "Paper copies are not recalled when a drawing changes.", "Replace the copy and check the other benches", "Stamp printed drawings with an expiry date"),
        InProgress(Severity.Critical, "Crack indication in lifting lug weld", "Dye penetrant shows a 12 mm linear indication at the toe of the lug weld on frame 7.", "Welding bay 1",
            "The lug was welded cold; preheat was skipped on the night shift.", "Quarantine frame 7 and excavate the indication", "Retrain the night shift on the preheat requirement"),
        Closed(Severity.Major, "Powder coat too thin on guard panels", "Thickness 35–45 µm against a 60 µm minimum on a rack of twelve guards.", "Powder coating line",
            "Rack earth contact was coated over, so the parts charged poorly.", "Strip and recoat the twelve guards", "Clean rack contacts every week"),
        Investigating(Severity.Major, "Intermittent short on wiring harness WH-3", "Harness fails the continuity test about one time in five at connector J2.", "Electrical test bench"),
        Pending(Severity.Minor, "Operator stamp missing on route cards", "Four route cards for job 5521 have no stamp against the deburr operation.", "Machine shop office",
            "The stamp for the new starter had not been issued yet.", "Confirm deburr was done and stamp the cards", "Issue stamps on a starter's first day"),
        Cancelled(Severity.Minor, "Pallet wrap torn on delivery", "Outer wrap torn on one pallet of cardboard sleeves.", "Goods inwards",
            "Only the wrap was damaged; the goods were inspected and are fine."),
        InProgress(Severity.Major, "Spot weld pull test below minimum", "Two of five pull tests on panel P-31 failed below 2.4 kN.", "Spot welding cell",
            "Electrode tips had mushroomed; tip dressing was skipped.", "Re-weld the panels made since the last good test", "Fit a counter that stops the cell for tip dressing"),
        Closed(Severity.Minor, "Wrong pallet label on finished goods", "Pallet 0931 carries a label for the previous order.", "Dispatch",
            "Labels were printed in a batch ahead of time and got out of order.", "Relabel pallet 0931", "Print each label as the pallet is closed"),
        Pending(Severity.Critical, "Brake pipe flare cracked", "Hairline crack in the flare of two brake pipes from the forming machine.", "Pipe forming",
            "The flaring die was chipped.", "Scrap every pipe formed since the last good check", "Replace the die and inspect it at each setup"),
        Rework(Severity.Minor, "Oil stain on upholstery panel", "Hydraulic oil spots on three seat backs after the press fit operation.", "Trim shop",
            "A hose on the press weeps onto the fixture.", "Clean or replace the three seat backs", "Replace the weeping hose",
            "The hose was replaced, but one of the three seat backs is still stained. Replace it before this is closed."),
        Open(Severity.Minor, "Chipped edge on glass shelf", "Small chip on the front edge of one shelf in a pack of ten.", "Goods inwards"),
        InProgress(Severity.Major, "Bearing pressed in skewed", "Bearing on idler I-5 sits 0.4 mm proud on one side; it runs rough.", "Sub-assembly bench 3",
            "The press tool has no pilot, so the bearing is started by eye.", "Strip and rebuild the affected idlers", "Make a piloted press tool"),
        Pending(Severity.Major, "Coating blisters after salt spray", "Blistering at 240 hours on panels that must pass 480.", "Test laboratory",
            "The pretreatment bath concentration was below its limit.", "Hold the lot and recoat it after pretreatment", "Titrate the bath daily and record it"),
        Investigating(Severity.Critical, "Pressure relief valve set too high", "Valve on receiver R-2 lifted at 11.8 bar; the nameplate says 10 bar.", "Compressor house"),
        Closed(Severity.Major, "Mixed part numbers in one bin", "Left- and right-hand hinges found in the same bin at the line.", "Assembly line 1",
            "Both hands arrive in identical unmarked bags.", "Sort the bin and check the doors built today", "Ask the supplier to colour-code the bags"),
        InProgress(Severity.Minor, "Gauge used past its calibration date", "Height gauge HG-03 has a sticker dated two weeks ago.", "Inspection room",
            "The reminder email goes to someone who has left.", "Calibrate HG-03 and review what it measured", "Send calibration reminders to the team mailbox"),
        Open(Severity.Minor, "Light scuffing on anodised trim", "Scuff marks on anodised trim strips where they touch in the stillage.", "Assembly line 2"),
        Investigating(Severity.Major, "Casting porosity opened up by machining", "Porosity exposed on the sealing face of three pump bodies.", "CNC cell 3"),
        Pending(Severity.Minor, "Instruction leaflet missing from boxes", "Six boxes in a sample of thirty have no leaflet.", "Packing line 1",
            "The leaflet magazine ran empty without any alarm.", "Open and check the boxes packed this shift", "Fit a low-level sensor to the magazine"),
        InProgress(Severity.Major, "Sealant cure time not observed", "Tanks were pressure tested four hours after sealing; the datasheet requires twelve.", "Tank assembly",
            "The cure time is not on the job card.", "Re-test the tanks after a full cure", "Add the cure time and a sign-off to the job card"),
        Open(Severity.Minor, "Fastener bag count short by one", "A bag marked 100 pcs of M6 screws held 99.", "Stores, aisle A"),
        Cancelled(Severity.Major, "Motor noise on fan unit", "Fan unit FU-12 was reported as noisy at the end-of-line test.", "End-of-line test",
            "The noise came from the test fixture; the unit passed on the second fixture."),
        InProgress(Severity.Critical, "Earth bond test failed on control cabinet", "Earth bond resistance 0.3 Ω against a 0.1 Ω limit on cabinet CC-8.", "Electrical assembly",
            "Paint was not removed under the earth stud.", "Clean the earth point to bare metal and re-test", "Mask earth points before painting"),
        Open(Severity.Major, "Hose routed against a sharp edge", "Coolant hose rubs on the edge of the bracket on unit 0457.", "Assembly line 2"),
        Investigating(Severity.Major, "Paint colour mismatch between batches", "Doors from batch 61 are visibly darker than the body panels.", "Paint shop exit"),
        Open(Severity.Minor, "Protective film left on a hidden face", "Film still on the inner face of two covers after assembly.", "Sub-assembly bench 1"),
        InProgress(Severity.Minor, "Deburring missed on cross-drilled hole", "Burr left at the intersection of the cross-drilled hole in valve block VB-2.", "CNC cell 2",
            "The deburr tool does not reach the intersection.", "Deburr the batch with a brush tool", "Add the brush tool to the machining program"),
        Investigating(Severity.Critical, "Guard removed from a running lathe", "Chuck guard on lathe L-4 was tied back while the machine ran.", "Machine shop"),
        Open(Severity.Critical, "Fork-lift damage to racking upright", "The upright at bay 14 is dented about 300 mm above the floor.", "Warehouse, bay 14"),
    ];

    private static DemoNcr Open(Severity severity, string title, string description, string location) =>
        new(DemoStage.Open, severity, title, description, location);

    private static DemoNcr Cancelled(Severity severity, string title, string description, string location, string reason) =>
        new(DemoStage.Cancelled, severity, title, description, location, Note: reason);

    private static DemoNcr Investigating(Severity severity, string title, string description, string location) =>
        new(DemoStage.Investigating, severity, title, description, location);

    private static DemoNcr InProgress(
        Severity severity, string title, string description, string location, string rootCause, string firstAction, string secondAction) =>
        new(DemoStage.ActionInProgress, severity, title, description, location, new DemoFix(rootCause, firstAction, secondAction));

    private static DemoNcr Rework(
        Severity severity, string title, string description, string location, string rootCause, string firstAction, string secondAction,
        string rejectionReason) =>
        new(DemoStage.ReworkAfterRejection, severity, title, description, location, new DemoFix(rootCause, firstAction, secondAction), rejectionReason);

    private static DemoNcr Pending(
        Severity severity, string title, string description, string location, string rootCause, string firstAction, string secondAction) =>
        new(DemoStage.PendingVerification, severity, title, description, location, new DemoFix(rootCause, firstAction, secondAction));

    private static DemoNcr Closed(
        Severity severity, string title, string description, string location, string rootCause, string firstAction, string secondAction) =>
        new(DemoStage.Closed, severity, title, description, location, new DemoFix(rootCause, firstAction, secondAction));
}
