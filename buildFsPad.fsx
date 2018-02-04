open System
open System.IO

let source = [
    "src/FsPad/TypeShape.fsx"
    "src/FsPad/Printer.fsx"
    "src/FsPad/WinForms.fsx"
]

let content = [ for file in source do
                    for line in File.ReadAllLines(file) do
                        if line.Contains("ignore-cat") = false then
                            yield line] |> String.concat "\n"

File.WriteAllText("FsPad.fsx", content.Replace("\r\n", "\n"), Text.UTF8Encoding.UTF8)

printfn "Wrote FsPad.fsx"