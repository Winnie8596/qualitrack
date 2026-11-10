type FormAlertProps = {
  message: string
}

// role="alert" makes a screen reader announce it as soon as it appears, without moving focus.
function FormAlert({ message }: FormAlertProps) {
  return (
    <p role="alert" className="rounded border border-red-300 bg-red-50 px-4 py-3 text-red-900">
      {message}
    </p>
  )
}

export default FormAlert
