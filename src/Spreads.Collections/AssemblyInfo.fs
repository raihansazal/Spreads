﻿namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("Spreads.Collections")>]
[<assembly: AssemblyProductAttribute("Spreads")>]
[<assembly: AssemblyDescriptionAttribute("Spreads")>]
[<assembly: AssemblyVersionAttribute("0.6.0-beta2")>]
[<assembly: AssemblyFileVersionAttribute("0.6.0-beta2")>]
[<assembly: AssemblyCopyrightAttribute("(c) Victor Baybekov 2016")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "0.6.0-beta2"
