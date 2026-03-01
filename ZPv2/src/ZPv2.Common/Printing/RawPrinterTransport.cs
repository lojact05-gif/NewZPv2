using System.Runtime.InteropServices;
using ZPv2.Common.Models;

namespace ZPv2.Common.Printing;

public sealed class RawPrinterTransport
{
    public PrintResult Send(string printerName, byte[] data, string docName = "ZPv2 Print Job")
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return new PrintResult { Ok = false, Message = "Impressora não informada." };
        }

        if (data is null || data.Length == 0)
        {
            return new PrintResult { Ok = false, Message = "Job de impressão vazio." };
        }

        IntPtr hPrinter = IntPtr.Zero;
        IntPtr pUnmanagedBytes = IntPtr.Zero;

        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                return new PrintResult { Ok = false, Message = "Não foi possível abrir a impressora: " + printerName };
            }

            var docInfo = new DOCINFOA
            {
                pDocName = docName,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, docInfo))
            {
                return new PrintResult { Ok = false, Message = "Falha ao iniciar documento RAW." };
            }

            if (!StartPagePrinter(hPrinter))
            {
                EndDocPrinter(hPrinter);
                return new PrintResult { Ok = false, Message = "Falha ao iniciar página RAW." };
            }

            pUnmanagedBytes = Marshal.AllocCoTaskMem(data.Length);
            Marshal.Copy(data, 0, pUnmanagedBytes, data.Length);

            if (!WritePrinter(hPrinter, pUnmanagedBytes, data.Length, out var written) || written != data.Length)
            {
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                return new PrintResult { Ok = false, Message = "Falha ao enviar bytes para a impressora." };
            }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
            return new PrintResult { Ok = true, Message = "Impresso com sucesso." };
        }
        catch (Exception ex)
        {
            return new PrintResult { Ok = false, Message = "Erro de spooler: " + ex.Message };
        }
        finally
        {
            if (pUnmanagedBytes != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pUnmanagedBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName = string.Empty;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
    }

    [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOA di);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
