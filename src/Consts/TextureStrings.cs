using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace MoreHealing.Consts;

public class TextureStrings
{
    #region Misc
    public const string QuickerFocusKey = "QuickerFocus";
    private const string QuickerFocusFile = "MoreHealing.Resources.QuickerFocus.png";
    public const string QuickestFocusKey = "QuickestFocus";
    private const string QuickestFocusFile = "MoreHealing.Resources.QuickestFocus.png";
    public const string DeeperFocusKey = "DeeperFocus";
    private const string DeeperFocusFile = "MoreHealing.Resources.DeeperFocus.png";
    public const string DeepestFocusKey = "DeepestFocus";
    private const string DeepestFocusFile = "MoreHealing.Resources.DeepestFocus.png";
    #endregion Misc

    private readonly Dictionary<string, Sprite> _dict;

    public TextureStrings()
    {
        Assembly asm = Assembly.GetExecutingAssembly();
        _dict = new Dictionary<string, Sprite>();
        Dictionary<string, string> tmpTextures = new Dictionary<string, string>();
        tmpTextures.Add(QuickerFocusKey, QuickerFocusFile);
        tmpTextures.Add(QuickestFocusKey, QuickestFocusFile);
        tmpTextures.Add(DeeperFocusKey, DeeperFocusFile);
        tmpTextures.Add(DeepestFocusKey, DeepestFocusFile);
        foreach (var t in tmpTextures)
        {
            using (Stream s = asm.GetManifestResourceStream(t.Value))
            {
                if (s == null) continue;

                byte[] buffer = new byte[s.Length];
                s.Read(buffer, 0, buffer.Length);
                s.Dispose();

                //Create texture from bytes
                var tex = new Texture2D(2, 2);

                tex.LoadImage(buffer, true);

                // Create sprite from texture
                // Split is to cut off the TestOfTeamwork.Resources. and the .png
                _dict.Add(t.Key, Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
            }
        }
    }

    public Sprite Get(string key)
    {
        return _dict[key];
    }
}