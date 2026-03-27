using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using QRCoder;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Serviço responsável pela geração de QR Codes para NFC-e.
    /// </summary>
    public class QrCodeService
    {
        /// <summary>
        /// Gera um QR Code a partir da URL/conteúdo fornecido e retorna como array de bytes PNG.
        /// </summary>
        public byte[] GerarQrCodeBytes(string conteudo, int tamanhoPixels = 200)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                throw new ArgumentException("Conteúdo do QR Code não pode ser vazio.", nameof(conteudo));

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(conteudo, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new BitmapByteQRCode(qrCodeData);
            return qrCode.GetGraphic(tamanhoPixels / qrCodeData.ModuleMatrix.Count);
        }

        /// <summary>
        /// Gera um QR Code e salva em arquivo PNG temporário, retornando o caminho.
        /// </summary>
        public string GerarQrCodeArquivo(string conteudo, int tamanhoPixels = 200)
        {
            var bytes = GerarQrCodeBytes(conteudo, tamanhoPixels);
            var tempPath = Path.Combine(Path.GetTempPath(), $"qrcode_{Guid.NewGuid():N}.png");
            File.WriteAllBytes(tempPath, bytes);
            return tempPath;
        }

        /// <summary>
        /// Gera um QR Code e retorna como System.Drawing.Bitmap.
        /// </summary>
        public Bitmap GerarQrCodeBitmap(string conteudo, int tamanhoPixels = 200)
        {
            if (string.IsNullOrWhiteSpace(conteudo))
                throw new ArgumentException("Conteúdo do QR Code não pode ser vazio.", nameof(conteudo));

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(conteudo, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(tamanhoPixels / qrCodeData.ModuleMatrix.Count);

            using var ms = new MemoryStream(pngBytes);
            return new Bitmap(ms);
        }

        /// <summary>
        /// Gera um QR Code e retorna como MemoryStream PNG.
        /// </summary>
        public MemoryStream GerarQrCodeStream(string conteudo, int tamanhoPixels = 200)
        {
            var bytes = GerarQrCodeBytes(conteudo, tamanhoPixels);
            return new MemoryStream(bytes);
        }
    }
}
