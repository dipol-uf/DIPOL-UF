using System;

namespace DIPOL_UF
{
    public class DeviceSettingsDescriptor
    {
        public string Name { get; set; }
        public string Alias { get; set; }
        public string Filter { get; set; }
        public int Order { get; set; }
        public string[] Reflect { get; set; }
        public RotationDescriptor Rotation { get; set; }

        public string[] ReflectEM { get; set; }

        public DipolImage.ReflectionDirection ReflectionDirection {
            get
            {
                static DipolImage.ReflectionDirection Parse(ReadOnlySpan<char> view)
                {
                    view = view.Trim();
                    if (view.Equals("horizontal".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        return DipolImage.ReflectionDirection.Horizontal;
                    }

                    if (view.Equals("vertical".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        return DipolImage.ReflectionDirection.Vertical;
                    }
                    return DipolImage.ReflectionDirection.NoReflection;
                }
                var flag = DipolImage.ReflectionDirection.NoReflection;
                if (Reflect is {Length: var len and > 0})
                {
                    switch (len)
                    {
                        case 1:
                            flag |= Parse(Reflect[0].AsSpan());
                            break;
                        case 2:
                            flag |= Parse(Reflect[0].AsSpan()) | Parse(Reflect[1].AsSpan());
                            break;
                    }
                }

                return flag;
            }
        }

        public DipolImage.ReflectionDirection ReflectionEMDirection
        {
            get
            {
                static DipolImage.ReflectionDirection Parse(ReadOnlySpan<char> view)
                {
                    view = view.Trim();
                    if (view.Equals("horizontal".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        return DipolImage.ReflectionDirection.Horizontal;
                    }

                    if (view.Equals("vertical".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    {
                        return DipolImage.ReflectionDirection.Vertical;
                    }
                    return DipolImage.ReflectionDirection.NoReflection;
                }
                var flag = DipolImage.ReflectionDirection.NoReflection;
                if (ReflectEM is { Length: var len and > 0 })
                {
                    switch (len)
                    {
                        case 1:
                            flag |= Parse(ReflectEM[0].AsSpan());
                            break;
                        case 2:
                            flag |= Parse(ReflectEM[0].AsSpan()) | Parse(ReflectEM[1].AsSpan());
                            break;
                    }
                }

                return flag;
            }
        }
        public DipolImage.RotateBy RotateImageBy =>
            Rotation?.RotateBy switch
            {
                90 => DipolImage.RotateBy.Deg90,
                180 => DipolImage.RotateBy.Deg180,
                270 => DipolImage.RotateBy.Deg270,
                _ => DipolImage.RotateBy.Deg0
            };
        public DipolImage.RotationDirection RotateImageDirection
        {
            get
            {
                var view = string.IsNullOrEmpty(Rotation?.RotationDirection)
                    ? default
                    : Rotation.RotationDirection.AsSpan().Trim();

                if (view.Equals("right".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    return DipolImage.RotationDirection.Right;
                }
                return DipolImage.RotationDirection.Left;
            }
        }
        public class RotationDescriptor
        {
            public int RotateBy { get; set; }
            public string RotationDirection { get; set; }
        }
    }

    
}
