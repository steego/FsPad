#load "SocketServer.fsx" //  ignore-cat
#load "ReflPrinter.fsx" // ignore-cat

open SocketServer
open ReflPrinter

let server = startServer(8080)

let dumpLevel (level:int) (value:'a) = ReflPrinter.print level value
let dump x = dumpLevel 3 x