// gapi-parser.cs - parsing driver application.
//
// Author: Mike Kestner <mkestner@novell.com>
//
// Copyright (c) 2005 Novell, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of version 2 of the GNU General Public
// License as published by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.

// Modified Feb 2020 by Marcello Calligaris
// - Use System.Diagnostics.Process.Start() instead of libc system()
// - replaced snake_case with camelCaps

using System.Threading.Tasks;

namespace GApi.Parser {

	using System;
	using System.Collections;
	using System.IO;
	using System.Xml;
    using System.Diagnostics;

	public static class Parser  
    {
		private static string _perl=null;
		private static string perl 
		{
			get{
				if(string.IsNullOrWhiteSpace(_perl))
					return Environment.OSVersion.Platform switch{
						 var x when (x == PlatformID.Win32NT 
						 	|| x==PlatformID.Win32S 
							 || x == PlatformID.Win32Windows
							 || x==PlatformID.WinCE) => "C:\\Perl64\\bin\\perl.exe",
						_=>"/usr/bin/perl"
					};
				else return _perl;
	
			}
		}
        public static async Task<int> Main (string[] args)
		{
			if (args.Length != 1) 
            {
				if(args.Length==2){
					Console.WriteLine ($"Using perl at {args[1]}");
					_perl = args[1];
				}
				else {
					Console.WriteLine ("Usage: gapi2-parser <filename> [path_to_perl_exec]");
					return 0;
				}
			}
			XmlDocument srcDoc = new XmlDocument ();

			try
            {
                using Stream stream = File.OpenRead (args [0]);
                srcDoc.Load (stream);
            } catch (XmlException e) {
				Console.WriteLine ("Couldn't open source file.");
				Console.WriteLine (e);
				return 1;
			}

			XmlNode root = srcDoc.DocumentElement;
			if (root.Name != "gapi-parser-input")
            {
				Console.WriteLine ("Improperly formatted input file: " + args [0]);
				return 1;
			}

			foreach (XmlNode apiNode in root.ChildNodes) 
            {
				if (apiNode.Name != "api")
					continue;

				string outFile = (apiNode as XmlElement).GetAttribute ("filename");
				string preFile = outFile + ".pre";

                if (File.Exists(preFile))
                    File.Delete (preFile);

				foreach (XmlNode libNode in apiNode.ChildNodes)
                {
					if (libNode.Name != "library")
						continue;

					string lib = (libNode as XmlElement).GetAttribute ("name");
			
					foreach (XmlNode nsNode in libNode.ChildNodes)
                    {
						if (nsNode.Name != "namespace")
							continue;

						string ns = (nsNode as XmlElement).GetAttribute ("name");
			
						ArrayList files = new ArrayList ();
						Hashtable excludes = new Hashtable ();

						foreach (XmlNode srcNode in nsNode.ChildNodes) 
                        {
							if (!(srcNode is XmlElement))
								continue;

							XmlElement elem = srcNode as XmlElement;

							switch (srcNode.Name) 
                            {
							case "dir":
								string dir = elem.InnerXml;
								Console.Write ("<dir {0}> ", dir);
								DirectoryInfo di = new DirectoryInfo (dir);
								foreach (FileInfo file in di.GetFiles ("*.c"))
									files.Add (dir + Path.DirectorySeparatorChar + file.Name);
								foreach (FileInfo file in di.GetFiles ("*.h"))
									files.Add (dir + Path.DirectorySeparatorChar + file.Name);
								break;
							case "file":
								string incFile = elem.InnerXml;
								Console.Write ("<file {0}> ", incFile);
								files.Add (incFile);
								break;
							case "exclude":
								string excFile = elem.InnerXml;
								Console.Write ("<exclude {0}> ", excFile);
								excludes [excFile] = 1;
								break;
							case "directory":
								string dirPath = elem.GetAttribute ("path");
								Console.Write ("<directory {0}: excluding ", dirPath);
								Hashtable excs = new Hashtable ();
								foreach (XmlNode excNode in srcNode.ChildNodes) {
									if (excNode.Name != "exclude")
										continue;
									string excFilename = (excNode as XmlElement).InnerXml;
									Console.Write (excFilename + " ");
									excs [excFilename] = 1;
								}
								DirectoryInfo dInfo = new DirectoryInfo (dirPath);
								foreach (FileInfo file in dInfo.GetFiles ("*.c")) {
									if (excs.Contains (file.Name))
										continue;
									files.Add (dirPath + Path.DirectorySeparatorChar + file.Name);
								}
								foreach (FileInfo file in dInfo.GetFiles ("*.h")) {
									if (excs.Contains (file.Name))
										continue;
									files.Add (dirPath + Path.DirectorySeparatorChar + file.Name);
								}
								Console.Write ("> ");
								break;
							default:
								Console.WriteLine ("Invalid source: " + srcNode.Name);
								break;
							}
						}

						Console.WriteLine ();

						if (files.Count == 0)
							continue;

						ArrayList realFiles = new ArrayList ();
						foreach (string file in files)
                        {
							string trimFile = file.TrimEnd ();
							if (excludes.Contains (trimFile))
								continue;

							realFiles.Add (trimFile);
						}
								
						string[] fileNames = (string[]) realFiles.ToArray (typeof (string));
						string ppArgs = string.Join (" ", fileNames);
                        string preScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gapi_pp.pl");
						string parseScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gapi2xml.pl");
						ProcessStartInfo ppInfo = new ProcessStartInfo(perl,$"{preScript} {ppArgs}")
                        {
							CreateNoWindow = false,
							UseShellExecute = false,
							RedirectStandardOutput = true,
							WorkingDirectory = Environment.CurrentDirectory

                        }, prInfo = new ProcessStartInfo(perl,$"{parseScript} {ns} {preFile} {lib}")
                        {
							CreateNoWindow = false,
							UseShellExecute = false,
							RedirectStandardInput = true,
							WorkingDirectory = Environment.CurrentDirectory
                        };
                        await ExternalStdIoPipe(ppInfo, prInfo);
                    }
				}
			
				XmlDocument final = new XmlDocument ();
				final.Load (preFile);
                XmlTextWriter writer = new XmlTextWriter(outFile, null)
                {
                    Formatting = Formatting.Indented
                };
                final.Save (writer);
				File.Delete (preFile);
			}

			return 0;
		}

        private static async Task ExternalStdIoPipe(ProcessStartInfo pipeSource, ProcessStartInfo pipeDest)
        {
            Process preProcess = Process.Start(pipeSource);
            Process parseProcess = Process.Start(pipeDest);

			if (preProcess != null && parseProcess!=null)
            {

				while (!preProcess.HasExited)
                {
                    string output = await preProcess.StandardOutput.ReadToEndAsync();
					await parseProcess.StandardInput.WriteAsync(output);
				}
				parseProcess.StandardInput.Flush();
				parseProcess.StandardInput.Close();
				parseProcess.StandardInput.Dispose();

                parseProcess.WaitForExit();
			} else throw new ApplicationException("Unable to start sub-process, Process.Start returned null");

        }

	}
}
