namespace Nucleus.Types
{
    public enum ImageOrientation
    {
        /// <summary>
        /// Renders the image with no care for the elements size.
        /// </summary>
        None,
        /// <summary>
        /// Renders the image at the center of the element; does not scale the image.
        /// </summary>
        Centered,
        /// <summary>
        /// Renders the image to stretch to the elements width and height. Does not retain aspect ratio.
        /// </summary>
        Stretch,
        /// <summary>
        /// Renders the image to fit within the elements bounds. Retains aspect ratio, but is able to scale past the textures size. 
        /// </summary>
        Zoom,
        /// <summary>
        /// Similar to Zoom's functionality, retains aspect ratio, but is unable to scale past the textures original size.
        /// </summary>
        Fit,
    }
}
