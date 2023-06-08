using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class TextureFactory
    {
        public enum FileSaveFormat
        {
            PNG,
            JPG,
            TGA
        }

        /// <summary>
        /// Creates a gradient texture with height 1.
        /// </summary>
        /// <param name="width">The desired width of the texture.</param>
        /// <param name="gradientStops">The gradient stops of the gradient.</param>
        /// <param name="format">The desired texture format. Default is RGBA32</param>
        /// <returns>The gradient texture as a Texture2D.</returns>
        public static Texture2D Create1DGradientTexture(int width, SortedDictionary<float, Color> gradientStops,
            TextureFormat format = TextureFormat.RGBA32)
        {
            return Create2DGradientTexture(width, 1, gradientStops, format);
        }

        /// <summary>
        /// Creates a gradient texture with custom width and height.
        /// </summary>
        /// <param name="width">The desired width of the texture.</param>
        /// <param name="height">The desired height of the texture.</param>
        /// <param name="gradientStops">The gradient stops of the gradient.</param>
        /// <param name="format">The desired texture format. Default is RGBA32</param>
        /// <returns>The gradient texture as a Texture2D.</returns>
        public static Texture2D Create2DGradientTexture(int width, int height,
            SortedDictionary<float, Color> gradientStops, TextureFormat format = TextureFormat.RGBA32)
        {
            // Validate texture dimensions
            if (width <= 0 || height <= 0)
            {
                PLog.Error<HotspotLogger>(
                    "[TextureFactory, Create1DGradientTexture] Width and height must be greater than 0! Returning null.");
                return null;
            }

            // Validate gradient stops
            if (gradientStops.Count == 0)
            {
                PLog.Error<HotspotLogger>(
                    "[TextureFactory, Create2DGradientTexture] Gradient texture must have at least 1 stop.");
                return null;
            }

            // Make sure there is a begin and end stop
            if (gradientStops.First().Key > 0)
                gradientStops.Add(0, gradientStops.First().Value);
            if (gradientStops.Last().Key < 1)
                gradientStops.Add(1, gradientStops.Last().Value);

            Texture2D gradientTexture = new Texture2D(width, height, format, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Mirror
            };

            // Pre-calculate gradient colors for performance optimization
            var gradientColors = new List<Color>(width);
            var previousStop = new KeyValuePair<float, Color>(0, gradientStops[0]);
            var nextStop = new KeyValuePair<float, Color>(1, gradientStops[gradientStops.Keys.Max()]);
            for (int i = 0; i < width; i++)
            {
                float currentStopVal = (float)i / width;

                // Get the correct stop
                foreach (var stop in gradientStops)
                {
                    if (stop.Key <= currentStopVal && stop.Key > previousStop.Key)
                    {
                        previousStop = stop;
                    }

                    if (stop.Key > currentStopVal)
                    {
                        nextStop = stop;
                        break;
                    }
                }

                // Calculate the interpolation value and store the gradient color
                float interpolationValue = (currentStopVal - previousStop.Key) / (nextStop.Key - previousStop.Key);
                gradientColors.Add(Color.Lerp(previousStop.Value, nextStop.Value, interpolationValue));
            }

            // Assign the color to the pixels in texture
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    gradientTexture.SetPixel(x, y, gradientColors[x]);
                }
            }

            gradientTexture.Apply();

            return gradientTexture;
        }

        /// <summary>
        /// Save a gradient texture as a desired format.
        /// </summary>
        /// <param name="texture">The texture to save.</param>
        /// <param name="path">The path to save the texture to.</param>
        /// <param name="format">The desired file format.</param>
        /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
        /// <remarks>
        /// Ensure that the path is rooted and valid! It should also contain the file name.
        /// </remarks>
        /// <returns>Whether the save was successful0</returns>
        public static bool SaveGradientTexture(Texture2D texture, string path, FileSaveFormat format,
            bool overwrite = false)
        {
            // Check for rooted path
            if (!Path.IsPathRooted(path))
            {
                PLog.Error<HotspotLogger>("[TextureFactory, SaveGradientTextureAsPNG] Path must be rooted!");
                return false;
            }

            // Check the file name
            string fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                PLog.Error<HotspotLogger>("[TextureFactory, SaveGradientTextureAsPNG] File name must not be empty!");
                return false;
            }

            // Check for format extension
            string formatExtension = format.ToString().ToLowerInvariant();
            if (!fileName.EndsWith(formatExtension))
            {
                path.RemoveLast(Path.GetExtension(path));
                path += formatExtension;
            }

            // If the file already exists and overwrite is disabled, return false
            if (File.Exists(path) && !overwrite)
            {
                PLog.Error<HotspotLogger>(
                    "[TextureFactory, SaveGradientTextureAsPNG] File already exists and overwrite is disabled! Path: " +
                    path
                );
                return false;
            }

            byte[] bytes;
            switch (format)
            {
                case FileSaveFormat.PNG:
                    bytes = texture.EncodeToPNG();
                    break;
                case FileSaveFormat.JPG:
                    bytes = texture.EncodeToJPG();
                    break;
                case FileSaveFormat.TGA:
                    bytes = texture.EncodeToTGA();
                    break;
                default:
                    PLog.Error<HotspotLogger>(
                        "[TextureFactory, SaveGradientTextureAsPNG] Unsupported format: " + format);
                    return false;
            }

            File.WriteAllBytes(path, bytes);
            PLog.Info<HotspotLogger>("[TextureFactory, SaveGradientTextureAsPNG] Saved texture to: " + path);
            return true;
        }

        /// <summary>
        /// Save a gradient texture as a PNG file.
        /// </summary>
        /// <param name="texture">The texture to save.</param>
        /// <param name="path">The path to save the texture to.</param>
        /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
        /// <remarks>
        /// Ensure that the path is rooted and valid! It should also contain the file name.
        /// </remarks>
        /// <returns>Whether the save was successful0</returns>
        public static bool SaveGradientTextureAsPNG(Texture2D texture, string path, bool overwrite = false)
        {
            return SaveGradientTexture(texture, path, FileSaveFormat.PNG, overwrite);
        }

        /// <summary>
        /// Save a gradient texture as a JPG file.
        /// </summary>
        /// <param name="texture">The texture to save.</param>
        /// <param name="path">The path to save the texture to.</param>
        /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
        /// <remarks>
        /// Ensure that the path is rooted and valid! It should also contain the file name.
        /// </remarks>
        /// <returns>Whether the save was successful0</returns>
        public static bool SaveGradientTextureAsJPG(Texture2D texture, string path, bool overwrite = false)
        {
            return SaveGradientTexture(texture, path, FileSaveFormat.JPG, overwrite);
        }

        /// <summary>
        /// Save a gradient texture as a TGA file.
        /// </summary>
        /// <param name="texture">The texture to save.</param>
        /// <param name="path">The path to save the texture to.</param>
        /// <param name="overwrite">Whether to overwrite the file if it already exists.</param>
        /// <remarks>
        /// Ensure that the path is rooted and valid! It should also contain the file name.
        /// </remarks>
        /// <returns>Whether the save was successful0</returns>
        public static bool SaveGradientTextureAsTGA(Texture2D texture, string path, bool overwrite = false)
        {
            return SaveGradientTexture(texture, path, FileSaveFormat.TGA, overwrite);
        }
    }
}