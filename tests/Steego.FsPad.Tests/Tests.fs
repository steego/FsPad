module Steego.FsPad.Tests

open NUnit.Framework

[<Test>]
let ``1 + 1 = 2`` () =
  let result = 1 + 1
  printfn "%i" result
  Assert.AreEqual(2,result)
