

module TypeInfo = begin
    open System
    open System.Collections
    open System.Reflection
    open System.Linq

    let inline isNotNull o = not (isNull o)

    /// Gets the cache info
    let cacheTypeInfo<'a>(getTypeInfo: Type -> 'a) = 
        let hashTable = System.Collections.Hashtable()
        let getType(t:Type) = begin
            let result = hashTable.[t]
            if isNotNull result then result :?> 'a
            else
                let newEntry = getTypeInfo(t)
                lock hashTable (fun () ->
                    hashTable.Add(t, newEntry)
                )
                newEntry
        end
        getType

    let isPrimitiveType(t:Type) = isNotNull t && (t.IsValueType || t = typeof<string>)
    let isPrimitiveObject(o:obj) = (isNull o || isPrimitiveType(o.GetType()))
    let isEnumerable(e:obj) =
        match e with
        | null -> false
        | :? string -> false
        | :? IEnumerable -> true
        | _ -> false

    let isSeq(t:System.Type) = 
        t <> null &&
        t <> typeof<string> &&
        t.GetInterface(typeof<System.Collections.IEnumerable>.Name) |> isNull |> not

    let isEnumerableType(t:Type) = if isNotNull t then isSeq(t) && t <> typeof<string> else false

    let private implementsGeneric<'a>(t:Type) = 
      let find = typedefof<'a>.GetGenericTypeDefinition()
      if isNull t then false
      elif not t.IsGenericType then false
      elif t.IsInterface && t.GetGenericTypeDefinition() = find then true
      else
        t.GetInterfaces()
        |> Seq.filter(fun i -> i.IsGenericType)
        |> Seq.map(fun i -> i.GetGenericTypeDefinition())
        |> Seq.exists(fun i -> i = find)

    let isGenericSeq(t:Type) = implementsGeneric<seq<_>>(t)

    type MemberGetter(name:string, memberType:Type, getter:Func<obj,obj>) = 
      member this.Name = name
      member this.Type = memberType
      member this.Get(o) = getter.Invoke(o)
      member this.IsEnumerable = isSeq(memberType)

    let getPropertyValue(o:obj, p:PropertyInfo) = 
        try p.GetValue(o, null)
        with ex -> null

    let getMemberGetters(t:Type) =
        if isNull t then []
        else
            let isGoodProperty (p:PropertyInfo) = 
                (not p.IsSpecialName)
                && p.GetIndexParameters().Length = 0
                && p.CanRead
                
            let flags = BindingFlags.Public ||| BindingFlags.Instance
            
            [ 
                // Return properties
                for p in t.GetProperties(flags) do
                    if isGoodProperty p then
                        yield MemberGetter(p.Name, p.PropertyType, fun o -> getPropertyValue(o, p))
                // Return fields
                for f in t.GetFields() do
                    if f.IsPublic && not f.IsSpecialName && not f.IsStatic then
                        yield MemberGetter(f.Name, f.FieldType, fun o -> f.GetValue(o))
            ]

    let getElementType(t:Type) = 
        if isNull t then null 
        else t.GetGenericArguments().FirstOrDefault()

    type TypeInfo(t:Type) = 
        let elementType = getElementType(t)
        let members = getMemberGetters(t)
        let isEnumerable = isEnumerableType(t)
        member this.Name = t.Name
        member this.IsPrimitive = isPrimitiveType(t)
        member this.IsNull = isNull t
        member this.Members = members
        member this.PrimitiveMembers = 
            members |> List.filter(fun m -> isPrimitiveType m.Type)
        member this.ObjectMembers = 
            members 
            |> List.filter(fun m -> (not (isPrimitiveType m.Type)))
            |> List.filter(fun m -> (not (isSeq m.Type)))
        member this.EnumerableMembers = members |> List.filter(fun m -> isSeq m.Type)
        member this.IsSeq = isEnumerable
        member this.IsGenericSeq = isGenericSeq(t)
        member this.ElementType = TypeInfo(elementType)
        new(o:obj) = TypeInfo(if isNull o then null else o.GetType())

    let getObjectInfo(o:obj) = if isNull o then [] 
                               else TypeInfo(o.GetType()).Members


end

module TypePatterns = begin
    open System
    open System.Linq
    open System.Collections
    open System.Collections.Generic


    let primitiveTypes = [
        typeof<int>; typeof<string>; typeof<DateTime>; typeof<bigint>; typeof<uint32>; typeof<char>;
        typeof<double>; typeof<decimal>; typeof<uint32>; typeof<byte>; typeof<float>; typeof<sbyte>; 
        typeof<int16>; typeof<uint64>; typeof<uint16>; typeof<bool>  
    ]

    let (|Primitive|)(t:Type) = 
        if primitiveTypes.Contains(t) then Some(t) else None

    let (|GenericType|_|) (t:Type) = 
        if t.IsGenericType then Some(t.GetGenericArguments() |> List.ofArray)
        else None

    // let (|Implements|_|) (find:Type) (t:Type) = 
    //     if t = find || t.Implements(find) then Some() else None

    let (|GenericInterface|_|) (find:Type) (t:Type) = 
      if isNull t then None
      elif not t.IsGenericType then None
      elif t.IsInterface && t.GetGenericTypeDefinition() = find then 
        Some([ for a in t.GetGenericArguments() -> a ])
      else
        Seq.head <| seq { 
          for i in t.GetInterfaces() do
            if i.IsGenericType && i.GetGenericTypeDefinition() = find then 
              yield Some([ for a in i.GetGenericArguments() -> a ])
          yield None 
        }

    module GenericTypes = 
        let listType = typedefof<List<_>>
        let seqType = typedefof<seq<_>>
        let dictType = typedefof<IDictionary<_,_>>
        let arrayType = typedefof<_[]>
        let nullableType = typedefof<Nullable<_>>
        let optionType = typedefof<Option<_>>

    module NonGenericTypes = 
        let dictType = typedefof<IDictionary>
        let enumerableType = typedefof<IEnumerable>

    // let (|SimpleEnumerable|_|) (t:Type) = 
    //     match t with
    //     | Implements NonGenericTypes.enumerableType as t -> Some(t)
    //     | _ -> None

    let (|IEnumerable|_|) = function
        | GenericInterface GenericTypes.seqType [t] -> Some(t)
        | GenericInterface GenericTypes.listType [t] -> Some(t)
        | _ -> None

    // let (|UntypedDictionary|_|) (t:Type) = 
    //     match t with
    //     | Implements NonGenericTypes.dictType as t -> Some(t)
    //     | _ -> None

    let (|TypedDictionary|_|) = function
        | GenericInterface GenericTypes.dictType [t] -> Some(t)
        | _ -> None



    // let (|SimpleObject|_|) = function
    //     | null -> None
    //     | Primitive(_) -> None
    //     | IEnumerable(_) -> None
    //     | SimpleEnumerable(_) -> None
    //     | t -> 
    //         let members = TypeInfo(t).Members
    //         let primitiveMembers = members |> List.filter(fun m -> isPrimitiveType m.Type)
    //         let objectMembers = members |> List.filter(fun m -> (not (isPrimitiveType m.Type)) && (not (isSeq m.Type)))
    //         let enumerableMembers = members |> List.filter(fun m -> isSeq m.Type)
    //         Some(members, primitiveMembers, objectMembers, enumerableMembers)

end

/////////////////////////////////////////////////////////////////////
//                         OBJECT PATTERNS                         //
/////////////////////////////////////////////////////////////////////

module ObjectPatterns = begin

    open System
    open System.Linq
    open System.Collections
    open System.Collections.Generic
    open TypeInfo
    open TypePatterns

    let (|IsSeq|_|) (candidate : obj) =
        if isNull candidate then None
        else begin
            let t = candidate.GetType()
            if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<seq<_>>
            then Some (candidate :?> System.Collections.IEnumerable)
            else None
        end

    ///////////////////////////////////////////////////////

    let (|IsNullable|_|) (candidate : obj) =
        let t = candidate.GetType()
        if t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<Nullable<_>>
        then Some (candidate)
        else None

    let inline isNotNull o = not (isNull o)
    let isPrimitiveType(t:Type) = isNotNull t && (t.IsValueType || t = typeof<string>)
    let isPrimitiveObject(o:obj) = (isNull o || isPrimitiveType(o.GetType()))

    let (|IsPrimitive|_|) (candidate : obj) =
        if isPrimitiveObject(candidate) then Some(candidate)
        else None

    let private implementsGeneric<'a>(t:Type) = 
      let find = typedefof<'a>.GetGenericTypeDefinition()
      if isNull t then false
      elif not t.IsGenericType then false
      elif t.IsInterface && t.GetGenericTypeDefinition() = find then true
      else
        t.GetInterfaces()
        |> Seq.filter(fun i -> i.IsGenericType)
        |> Seq.map(fun i -> i.GetGenericTypeDefinition())
        |> Seq.exists(fun i -> i = find)

    let isGenericSeq(t:Type) = implementsGeneric<seq<_>>(t)

    let (|GenericList|_|)(o:obj) =
        if isNull o then None
        else
            let t = o.GetType()
            let ti = TypeInfo(t)
            if ti.IsGenericSeq then
                if not ti.IsPrimitive then
                    let getters = ti.ElementType.Members
                    let list = o :?> IEnumerable
                    Some(t, getters, list)
                else 
                    None
            elif t.IsArray && t.HasElementType then
                let ti = TypeInfo(t.GetElementType())
                if not ti.IsPrimitive then
                    let getters = ti.ElementType.Members
                    let list = o :?> IEnumerable
                    Some(t, getters, list)
                else 
                    None                
            else None

    let (|Object|_|)(o:obj) = 
        if isNull o then None
        elif isEnumerable(o) then None
        elif isPrimitiveObject(o) then None
        else
            let members = TypeInfo(o.GetType()).Members
            let primitiveMembers = members |> List.filter(fun m -> isPrimitiveType m.Type)
            let objectMembers = members |> List.filter(fun m -> (not (isPrimitiveType m.Type)) && (not (isSeq m.Type)))
            let enumerableMembers = members |> List.filter(fun m -> isSeq m.Type)
            Some(members, primitiveMembers, objectMembers, enumerableMembers, obj)
end
