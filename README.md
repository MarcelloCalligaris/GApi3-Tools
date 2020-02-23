# GApi3-Tools

`gapi3-parser`, `gapi3-fixup`, and `gapi3-codegen` for .NET Core 3.1

## Usage

### Requirements

* Perl with `XML::LibXML` (for gapi3-parser to run)
* .NET Core 3.1 SDK (for building)

## About

This repository is a stand alone toolset for building .NET wrappers for GLib Objects originally written in C.

GapiCodegen & GapiFixup were taken from [GtkSharp/GtkSharp](https://github.com/GtkSharp/GtkSharp/tree/949ee6771c3a127a44e4d7e7976d12e2cd96fc1b/Source/Tools).
GapiParser was taken from [mono/gtk-sharp](https://github.com/mono/gtk-sharp/tree/9dc89137ba1a5b2420523de379461928e82d0477/parser) and re-packaged for .NET core.

### Modifications

Minor modifications were made to GApi Parser to make it more Windows-friendly, primarily the use of `System.Diagnostics.Process` to run the perl scripts instead of using `system("gapi_pp.pl ... | gapi2xml.pl ...")`

In addition, some variable and method names were modified to be more inline with .NET naming conventions. Most of the logic remains untouched.