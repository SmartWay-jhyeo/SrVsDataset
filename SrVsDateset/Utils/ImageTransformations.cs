using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SrVsDataset.Models;

namespace SrVsDataset.Utils
{
    /// <summary>
    /// 이미지 회전 및 플립 변환 유틸리티
    /// </summary>
    public static class ImageTransformations
    {
        /// <summary>
        /// 이미지에 회전 및 플립 변환을 적용
        /// </summary>
        public static BitmapSource ApplyTransformations(BitmapSource source, ImageRotation rotation, ImageFlip flip)
        {
            if (source == null) return null;

            var transformedImage = source;

            // 회전 적용
            if (rotation != ImageRotation.Rotate0)
            {
                transformedImage = ApplyRotation(transformedImage, rotation);
            }

            // 플립 적용
            if (flip != ImageFlip.None)
            {
                transformedImage = ApplyFlip(transformedImage, flip);
            }

            return transformedImage;
        }

        /// <summary>
        /// 이미지 회전 적용
        /// </summary>
        private static BitmapSource ApplyRotation(BitmapSource source, ImageRotation rotation)
        {
            var transform = new RotateTransform((double)rotation);
            return new TransformedBitmap(source, transform);
        }

        /// <summary>
        /// 이미지 플립 적용
        /// </summary>
        private static BitmapSource ApplyFlip(BitmapSource source, ImageFlip flip)
        {
            Transform transform = flip switch
            {
                ImageFlip.Horizontal => new ScaleTransform(-1, 1),
                ImageFlip.Vertical => new ScaleTransform(1, -1),
                ImageFlip.Both => new ScaleTransform(-1, -1),
                _ => Transform.Identity
            };

            if (transform != Transform.Identity)
            {
                return new TransformedBitmap(source, transform);
            }

            return source;
        }

        /// <summary>
        /// TransformGroup을 사용하여 여러 변환을 한번에 적용
        /// </summary>
        public static BitmapSource ApplyTransformationsOptimized(BitmapSource source, ImageRotation rotation, ImageFlip flip)
        {
            if (source == null || (rotation == ImageRotation.Rotate0 && flip == ImageFlip.None))
                return source;

            var transformGroup = new TransformGroup();

            // 회전 변환 추가
            if (rotation != ImageRotation.Rotate0)
            {
                transformGroup.Children.Add(new RotateTransform((double)rotation));
            }

            // 플립 변환 추가
            if (flip != ImageFlip.None)
            {
                var scaleTransform = flip switch
                {
                    ImageFlip.Horizontal => new ScaleTransform(-1, 1),
                    ImageFlip.Vertical => new ScaleTransform(1, -1),
                    ImageFlip.Both => new ScaleTransform(-1, -1),
                    _ => null
                };

                if (scaleTransform != null)
                {
                    transformGroup.Children.Add(scaleTransform);
                }
            }

            return new TransformedBitmap(source, transformGroup);
        }
    }
}