using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MoreHealing.Consts;

public class TextureStrings
{
    #region Misc
    public const string QuickerFocusKey = "QuickerFocus";
    public const string QuickestFocusKey = "QuickestFocus";
    public const string DeeperFocusKey = "DeeperFocus";
    public const string DeepestFocusKey = "DeepestFocus";
    #endregion Misc

    private readonly string _dir;
    private readonly Dictionary<string, Sprite> _dict;

    public TextureStrings()
    {
        _dir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Sprites");

        Assembly asm = Assembly.GetExecutingAssembly();
        _dict = new Dictionary<string, Sprite>();
        List<string> tmpTextures = new List<string>();
        tmpTextures.Add(QuickerFocusKey);
        tmpTextures.Add(QuickestFocusKey);
        tmpTextures.Add(DeeperFocusKey);
        tmpTextures.Add(DeepestFocusKey);
        foreach (var t in tmpTextures)
        {
            //Create texture from bytes
            var tex = new Texture2D(2, 2);

            string filePath = Path.Combine(_dir, $"{t}.png");
            if (Directory.Exists(_dir) && File.Exists(filePath))
            {
                // when file exists, use that
                tex.LoadImage(File.ReadAllBytes(filePath));
            }
            else
            {
                // otherwise, embedded resource
                using (Stream s = asm.GetManifestResourceStream($"MoreHealing.Resources.{t}.png"))
                {
                    if (s == null) continue;

                    byte[] buffer = new byte[s.Length];
                    s.Read(buffer, 0, buffer.Length);
                    s.Dispose();


                    tex.LoadImage(buffer, true);
                }
            }

            // Create sprite from texture
            // Split is to cut off the TestOfTeamwork.Resources. and the .png
            _dict.Add(t, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
        }
    }

    public Sprite Get(string key)
    {
        return _dict[key];
    }
}