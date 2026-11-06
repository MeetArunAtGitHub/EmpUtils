using System;
using System.Drawing;
using System.Windows.Forms;

namespace EmpUtils
{
    internal static class AppBranding
    {
        // Shared for the process lifetime so every taskbar window uses the same icon.
        private static readonly Lazy<Icon> ApplicationIcon = new Lazy<Icon>(() =>
        {
            using (var stream = typeof(AppBranding).Assembly.GetManifestResourceStream("EmpUtils.ApplicationIcon"))
            {
                if (stream == null)
                    throw new InvalidOperationException("The EmpUtils application icon is missing.");
                using (var icon = new Icon(stream))
                    return (Icon)icon.Clone();
            }
        });

        internal static void Apply(Form form)
        {
            form.Icon = ApplicationIcon.Value;
        }
    }
}
