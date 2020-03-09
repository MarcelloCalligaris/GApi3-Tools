using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace GLibrary
{
    public class GLibrary
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        private static Dictionary<Library, IntPtr> _libraries;
        private static Dictionary<string, IntPtr> _customlibraries;
        private static Dictionary <Library, string[]> _libraryDefinitions;

        static GLibrary()
        {
            _customlibraries = new Dictionary<string, IntPtr>();
            _libraries = new Dictionary<Library, IntPtr>();
            _libraryDefinitions = new Dictionary<Library, string[]>();
            _libraryDefinitions[Library.GLib] = new[] { "libglib-2.0-0.dll", "libglib-2.0.so.0", "libglib-2.0.0.dylib", "glib-2.dll" };
            _libraryDefinitions[Library.GObject] = new[] { "libgobject-2.0-0.dll", "libgobject-2.0.so.0", "libgobject-2.0.0.dylib", "gobject-2.dll" };
            _libraryDefinitions[Library.Cairo] = new[] { "libcairo-2.dll", "libcairo.so.2", "libcairo.2.dylib", "cairo.dll" };
            _libraryDefinitions[Library.Gio] = new[] { "libgio-2.0-0.dll", "libgio-2.0.so.0", "libgio-2.0.0.dylib", "gio-2.dll" };
            _libraryDefinitions[Library.Atk] = new[] { "libatk-1.0-0.dll", "libatk-1.0.so.0", "libatk-1.0.0.dylib", "atk-1.dll" };
            _libraryDefinitions[Library.Pango] = new[] { "libpango-1.0-0.dll", "libpango-1.0.so.0", "libpango-1.0.0.dylib", "pango-1.dll" };
            _libraryDefinitions[Library.Gdk] = new[] { "libgdk-3-0.dll", "libgdk-3.so.0", "libgdk-3.0.dylib", "gdk-3.dll" };
            _libraryDefinitions[Library.GdkPixbuf] = new[] { "libgdk_pixbuf-2.0-0.dll", "libgdk_pixbuf-2.0.so.0", "libgdk_pixbuf-2.0.dylib", "gdk_pixbuf-2.dll" };
            _libraryDefinitions[Library.Gtk] = new[] { "libgtk-3-0.dll", "libgtk-3.so.0", "libgtk-3.0.dylib", "gtk-3.dll" };
            _libraryDefinitions[Library.PangoCairo] = new[] { "libpangocairo-1.0-0.dll", "libpangocairo-1.0.so.0", "libpangocairo-1.0.0.dylib", "pangocairo-1.dll" };

            // New Library Definitions 
            _libraryDefinitions[Library.GtkSourceView] =  new[] { "libgtksourceview-4-0.dll", "libgtksourceview-4.so.0", "libgtksourceview-4.0.dylib", "gtksourceview-4.dll" };
            _libraryDefinitions[Library.ActionsMenusToolbarsKit] = new[] { "libamtk-5-0.dll", "libamtk-5.so.0", "libamtk-5.0.dylib", "amtk-5.dll" };
            _libraryDefinitions[Library.TextEditorProductLine] = new[] { "libtepl-4-0.dll", "libtepl-4.so.0", "libtepl-4.0.dylib", "tepl-4.dll" };
            _libraryDefinitions[Library.GnomeDocking] = new[] { "libgdl-3-5.dll", "libgdl-3.so.4", "libgdl-3.5.dylib", "gdl-3.dll" };
        }

        public static IntPtr Load(Library library)
        {
            var ret = IntPtr.Zero;
            if (_libraries.TryGetValue(library, out ret))
                return ret;

            if (FuncLoader.IsWindows)
            {
                ret = FuncLoader.LoadLibrary(_libraryDefinitions[library][0]);

                if (ret == IntPtr.Zero)
                {
                    SetDllDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Gtk", "3.24"));
                    ret = FuncLoader.LoadLibrary(_libraryDefinitions[library][0]);
                }
            }
            else if (FuncLoader.IsOSX)
                ret = FuncLoader.LoadLibrary(_libraryDefinitions[library][2]);
            else
                ret = FuncLoader.LoadLibrary(_libraryDefinitions[library][1]);

            if (ret == IntPtr.Zero)
            {
                for (int i = 0; i < _libraryDefinitions[library].Length; i++)
                {
                    ret = FuncLoader.LoadLibrary(_libraryDefinitions[library][i]);

                    if (ret != IntPtr.Zero)
                        break;
                }
            }

            if (ret == IntPtr.Zero)
            {
                var err = library + ": " + string.Join(", ", _libraryDefinitions[library]);
                throw new DllNotFoundException(err);
            }

            _libraries[library] = ret;
            return ret;
        }
    }
}