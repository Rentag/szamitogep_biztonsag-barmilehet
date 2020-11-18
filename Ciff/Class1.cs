using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;


namespace Ciff
{
    public class ciff
    { 
        
        public void cifftopng (string input,string output)
        {
        
            string text = System.IO.File.ReadAllText(input);
            //System.Console.WriteLine("Contents of WriteText.txt = {0}", text);

            byte[] bytes= new byte[0];

            using (FileStream fsSource = new FileStream(input, FileMode.Open, FileAccess.Read))
            {

                bytes = new byte[fsSource.Length];
                int numBytesToRead = (int)fsSource.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                numBytesToRead = bytes.Length;


            }
            byte[] magic_bytes = new byte[4];
            Array.Copy(bytes, 0, magic_bytes, 0, 4);
            string magic = System.Text.Encoding.UTF8.GetString(magic_bytes);

            byte[] h_size_bytes = new byte[8];
            Array.Copy(bytes, 4, h_size_bytes, 0, 8);
            int h_size = BitConverter.ToInt32(h_size_bytes,0);

            byte[] c_size_bytes = new byte[8];
            Array.Copy(bytes, 12, c_size_bytes, 0, 8);
            int c_size = BitConverter.ToInt32(c_size_bytes,0);

            byte[] width_bytes = new byte[8];
            Array.Copy(bytes, 20, width_bytes, 0, 8);
            int width = BitConverter.ToInt32(width_bytes,0);

            byte[] height_bytes = new byte[8];
            Array.Copy(bytes, 28, height_bytes, 0, 8);
            int height = BitConverter.ToInt32(height_bytes,0);

            byte[] content_bytes = new byte[c_size];
            Array.Copy(bytes, h_size, content_bytes, 0, c_size);
       
            /*
            System.Console.WriteLine("magic = {0}", magic);
            System.Console.WriteLine("h_size = {0}", h_size);
            System.Console.WriteLine("c_size = {0}", c_size);
            System.Console.WriteLine("width = {0}", width);
            System.Console.WriteLine("height = {0}", height);
            */
           
            byte[] A_max = BitConverter.GetBytes(255);

            int index = 0;
            using (Image<Rgba32> image = new Image<Rgba32>(width, height))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Rgba32 pixel = image[x, y];
                        pixel.R = content_bytes[index++];
                        pixel.G = content_bytes[index++];
                        pixel.B = content_bytes[index++];
                        pixel.A = 255;
                        image[x, y] = pixel;
                    }
                }
                image.Save(output);
            }
        }
        
    }
}
