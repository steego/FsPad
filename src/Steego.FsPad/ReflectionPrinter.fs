namespace FsPad

#if INTERACTIVE
#load "PrettyPrinter.fsx"
#load "Reflection.fsx"
#endif

type UnprotectedHtml = UnprotectedHtml of string

module ReflectionPrinter = begin

    open System
    open System.Linq
    open PrettyPrinter

    open Reflection.TypePatterns
    open Reflection.ObjectPatterns

    let private formatDate(d:DateTime) = 
        sprintf "%02i-%02i-%02i %02i:%02i:%02i.%03i)" d.Year d.Month d.Day d.Hour d.Minute d.Second d.Millisecond

    let rec print (level:int) (o:obj) : PrettyPrint = 
        let printChild = print (level - 1)
        match o with
        | null                                              -> Value(null, "(null)")
        | :? UnprotectedHtml as h                           -> let (UnprotectedHtml(html)) = h in RawHtml(html)
        | :? unit                                           -> Value("Unit", "()")
        | :? bool as v                                      -> Value("Boolean", string(v))
        | :? System.Byte as v                               -> Value ("Byte", sprintf "%duy" v)
        | :? System.Int32 as v                              -> Value ("Int"  , string<int> v)
        | :? System.Int64 as v                              -> Value ("Int64", string<int64> v)
        | :? System.Double as v                             -> Value ("Float", string<float> v)
        | :? System.String as v                             -> Value ("String", v)
        | :? System.DateTime as d                           -> Value ("DateTime", formatDate(d))
        | IsPrimitive(n)                                    -> Value("Primitive", n.ToString())
        | IsNullable(n)                                     -> printChild n
        | GenericList(_) when level < 1                     -> MaxRecurse
        | :? System.Collections.IEnumerable when level < 1  -> MaxRecurse
        | Object(_) when level < 1                          -> MaxRecurse
        | _ when level < 1                                  -> MaxRecurse
        | GenericList(Primitive(_), _, list)                -> List([for item in list -> printChild item])
        | :? System.Collections.IEnumerable as list         -> List([for item in list.OfType<obj>() -> printChild item])
        | Object(members, _, _, _, _)                       -> Table([ for m in members -> { name = m.Name; value = printChild(m.Get(o))} ])
        | _ -> Value("Error", "Dunno know how!")

end