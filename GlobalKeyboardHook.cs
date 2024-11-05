using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

public class GlobalKeyboardHook : IDisposable
{
    public event EventHandler<string> BarcodeScanned;
    private readonly StringBuilder barcodeBuilder = new StringBuilder();
    private DateTime lastKeyPressTime = DateTime.Now;
    private readonly int barcodeInputTimeout = 100; // Tiempo máximo entre caracteres (en milisegundos)

    private IntPtr hookId = IntPtr.Zero;
    private bool disposed = false;

    public GlobalKeyboardHook()
    {
        hookId = SetHook(HookCallback);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        const int WM_KEYDOWN = 0x0100;

        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            char keyChar = (char)vkCode;

            // Detectar si el tiempo entre teclas excede el límite para un código de barras (indica entrada manual)
            var currentTime = DateTime.Now;
            if ((currentTime - lastKeyPressTime).TotalMilliseconds > barcodeInputTimeout)
            {
                barcodeBuilder.Clear(); // Reiniciar si la entrada es demasiado lenta
            }
            lastKeyPressTime = currentTime;

            // Procesar el carácter
            if (keyChar == '\r') // Enter key pressed, end of barcode
            {
                if (barcodeBuilder.Length > 0) // Solo procesar si tenemos un código acumulado
                {
                    // Eliminar espacios en blanco y disparar el evento con el código limpio
                    var barcode = barcodeBuilder.ToString().Replace(" ", string.Empty);
                    BarcodeScanned?.Invoke(this, barcode);
                    barcodeBuilder.Clear();
                }
            }
            else if (!char.IsControl(keyChar)) // Ignorar teclas de control como Shift y Alt
            {
                barcodeBuilder.Append(keyChar);
            }
        }

        return CallNextHookEx(hookId, nCode, wParam, lParam);
    }

    private const int WH_KEYBOARD_LL = 13;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    // Liberar el hook al finalizar
    public void Dispose()
    {
        if (!disposed)
        {
            UnhookWindowsHookEx(hookId);
            disposed = true;
        }
    }
}
