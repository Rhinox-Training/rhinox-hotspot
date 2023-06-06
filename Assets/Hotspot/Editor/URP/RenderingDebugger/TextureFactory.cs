﻿using System.Collections.Generic;
using System.Linq;
using Rhinox.Perceptor;
using UnityEngine;

namespace Hotspot.Editor
{
    public static class TextureFactory
    {
        /// <summary>
        /// Creates a gradient texture with height 1.
        /// </summary>
        /// <param name="width">The desired width of the texture</param>
        /// <param name="gradientStops">The gradient stops of the gradient</param>
        /// <returns></returns>
        public static Texture2D Create1DGradientTexture(int width, SortedDictionary<float, Color> gradientStops)
        {
            // Make sure there is a begin and end stop
            if (gradientStops.Count < 2)
            {
                PLog.Error<HotspotLogger>("[TextureFactory, Create1DGradientTexture] Gradient texture must have at least 2 stops");
                return null;
            }
            
            if(gradientStops.First().Key>0)
                gradientStops.Add(0, gradientStops.First().Value);
            if(gradientStops.Last().Key<1) 
                gradientStops.Add(1, gradientStops.Last().Value);
            
            Texture2D gradientTexture = new Texture2D(width, 1, TextureFormat.RG32, false);

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

                // Calculate the interpolation value
                float interpolationValue = (currentStopVal - previousStop.Key) / (nextStop.Key - previousStop.Key);

                // Assign the color to the pixel in texture
                gradientTexture.SetPixel(i, 0, Color.Lerp(previousStop.Value, nextStop.Value, interpolationValue));
            }

            gradientTexture.Apply();

            return gradientTexture;
        }
    }
}