namespace sharpLens.Client

open System
open Microsoft.FSharp.Reflection

module ReflectionHelper =

    let rec getProperties (obj: obj) : (string * string) list =
        if isNull obj then
            []
        else
            let t = obj.GetType()

            if FSharpType.IsUnion t then
                let case, fields = FSharpValue.GetUnionFields(obj, t)
                let fieldInfos = case.GetFields()

                let fieldProps =
                    fields
                    |> Array.mapi (fun i f ->
                        let propName = fieldInfos.[i].Name
                        (propName, sprintf "%A" f))
                    |> Array.toList

                ("Case", case.Name) :: fieldProps
            elif FSharpType.IsRecord t then
                FSharpType.GetRecordFields(t)
                |> Array.map (fun prop ->
                    let value = prop.GetValue(obj)
                    (prop.Name, sprintf "%A" value))
                |> Array.toList
            else
                t.GetProperties()
                |> Array.map (fun prop ->
                    let value = prop.GetValue(obj)
                    (prop.Name, sprintf "%A" value))
                |> Array.toList

    let getConstructorDetails (t: Type) =
        let methodInfo = t.GetMethod("New")

        if isNull methodInfo then
            None
        else
            let parameters = methodInfo.GetParameters()

            let paramDetails =
                parameters |> Array.map (fun p -> p.Name, p.ParameterType) |> Array.toList

            Some(methodInfo, paramDetails)

    let invokeConstructor (methodInfo: System.Reflection.MethodInfo) (args: obj[]) = methodInfo.Invoke(null, args)

    let getUnionCases (t: Type) =
        if FSharpType.IsUnion t then
            FSharpType.GetUnionCases t
            |> Array.map (fun case ->
                let fields = case.GetFields() |> Array.map (fun f -> f.Name, f.PropertyType)
                case, fields)
            |> Array.toList
        else
            []
