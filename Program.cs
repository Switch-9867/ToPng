using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebPWrapper;

namespace ToPng
{
	internal class Program
	{
		// BMP, GIF, EXIF, JPG, PNG and TIFF
		private static readonly string[] DEFAULT_SUPPORTED_FILE_TYPES = new string[] { ".bmp", ".gif", ".exif", ".jpg", ".png", ".tiff"};
		private static readonly string[] EXTENDED_SUPPORTED_FILE_TYPES = new string[] { ".webp" };

		private static string[] SUPPORTED_FILE_TYPES { get { return ConcatArrays(DEFAULT_SUPPORTED_FILE_TYPES, EXTENDED_SUPPORTED_FILE_TYPES); } }

		static void Main(string[] args)
		{
			AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;

			if (args.Length == 0) return;
			string filePath = args[0];
			if (!File.Exists(filePath)) { Console.WriteLine($"File {filePath} does not exist"); Console.ReadLine(); return; }
			if (!SUPPORTED_FILE_TYPES.Contains(Path.GetExtension(filePath))) { Console.WriteLine($"Unsupported file type: {Path.GetExtension(filePath)}"); Console.ReadLine(); return; }
			Image img = ImportImage(filePath);
			string savePath = Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".png");
			Console.WriteLine($"Saving to {savePath}");
			img.Save(savePath, ImageFormat.Png);
			img.Dispose();
			File.Delete(filePath);
		}

		private static Image ImportImage(string filePath)
		{
			string fileType = Path.GetExtension(filePath).ToLower();
			if (DEFAULT_SUPPORTED_FILE_TYPES.Contains(fileType)) return Image.FromFile(filePath);
			switch (fileType)
			{
				case ".webp":
					return WebpConverter(filePath);
				default:
					Console.WriteLine($"Unexpected file type: {fileType}");
					Console.ReadLine();
					Environment.Exit(0);
					return null;
			}
		}

		private static Image WebpConverter(string filePath)
		{
			try
			{
				byte[] rawWebP = File.ReadAllBytes(filePath);
				WebP webp = new WebP();
				return webp.Decode(rawWebP);
			}
			catch (Exception ex) { throw ex; }
		}

		private static T[] ConcatArrays<T>(T[] Array1, T[] Array2)
		{
			T[] TempArray = new T[Array1.Length + Array2.Length];
			Array1.CopyTo(TempArray, 0);
			Array2.CopyTo(TempArray, Array1.Length);
			return TempArray;
		}
		private static ImageCodecInfo GetEncoderInfo(String mimeType)
		{
			int j;
			ImageCodecInfo[] encoders;
			encoders = ImageCodecInfo.GetImageEncoders();
			for (j = 0; j < encoders.Length; ++j)
			{
				if (encoders[j].MimeType == mimeType)
					return encoders[j];
			}
			return null;
		}

		/// <summary>
		/// Hooks to assembly resolver and tries to load assembly (.dll)
		/// from executable resources it CLR can't find it locally.
		///
		/// Used for embedding assemblies onto executables.
		///
		/// See: http://www.digitallycreated.net/Blog/61/combining-multiple-assemblies-into-a-single-exe-for-a-wpf-application
		/// </summary>
		private static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
		{
			var executingAssembly = Assembly.GetExecutingAssembly();
			var assemblyName = new AssemblyName(args.Name);

			var path = assemblyName.Name + ".dll";
			if (!assemblyName.CultureInfo.Equals(CultureInfo.InvariantCulture))
			{
				path = $"{assemblyName.CultureInfo}\\${path}";
			}

			using var stream = executingAssembly.GetManifestResourceStream(path);
			if (stream == null)
				return null;

			var assemblyRawBytes = new byte[stream.Length];
			stream.Read(assemblyRawBytes, 0, assemblyRawBytes.Length);
			return Assembly.Load(assemblyRawBytes);
		}
	}
}
