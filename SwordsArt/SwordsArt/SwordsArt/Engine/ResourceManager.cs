using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using TiledLib;

namespace SwordsArt.Engine
{
    public static class ResourceManager
    {
        static string DIR_SEPARATOR = "/";

        private static Dictionary<string, Texture2D> texDic = new Dictionary<string, Texture2D>();
        private static Dictionary<string, SpriteFont> fontDic = new Dictionary<string, SpriteFont>();
        private static Dictionary<string, Color[,]> colDic = new Dictionary<string, Color[,]>();
        private static Dictionary<string, SoundEffect> soundDic = new Dictionary<string, SoundEffect>();
        private static Dictionary<string, SoundEffectInstance> soundInsDic = new Dictionary<string, SoundEffectInstance>();

        public static Texture2D Pixel { get { return GetTexture("pixel"); } }

        private static List<string> textureNames = new List<string>
            {
                "pixel", "Player", "Player_Basic"
            };
        private static List<string> fontNames = new List<string>
            {
                "TimeText"
            };
        private static List<string> soundNames = new List<string>
        {

        };
        private static List<string> soundInstanceNames = new List<string>
        {

        };

        public static void LoadContent(ContentManager content)
        {
            foreach (string name in textureNames)
            {
                Texture2D texture = content.Load<Texture2D>("Images/" + name);
                texDic[name] = texture;
                colDic[name] = TextureToColors(texture);
            }
            foreach (string name in fontNames)
            {
                SpriteFont font = content.Load<SpriteFont>("Fonts/" + name);
                fontDic[name] = font;
            }
            foreach (string name in soundNames)
            {
                SoundEffect sound = content.Load<SoundEffect>("Sounds/" + name);
                soundDic[name] = sound;
            }
            foreach (string name in soundInstanceNames)
            {
                SoundEffect sound = content.Load<SoundEffect>("Sounds/" + name);
                SoundEffectInstance si = sound.CreateInstance();
                soundInsDic[name] = si;
            }
        }

        public static SoundState insState(string name)
        {
            return soundInsDic[name].State;
        }

        public static void playSI(string name)
        {
            soundInsDic[name].Play();
        }

        public static void stopSI(string name)
        {
            soundInsDic[name].Stop();
        }

        public static void playSound(string name)
        {
            soundDic[name].Play();
        }

        public static SoundEffect GetSound(string name)
        {
            return soundDic[name];
        }

        public static SpriteFont GetFont(string name)
        {
            return fontDic[name];
        }

        public static Texture2D GetTexture(string name)
        {
            return texDic[name];
        }

        public static Color[,] GetColors(string name)
        {
            return colDic[name];
        }

        private static Color[,] TextureToColors(Texture2D texture)
        {
            Color[] colors1D = new Color[texture.Width * texture.Height];
            texture.GetData(colors1D);

            Color[,] colors2D = new Color[texture.Width, texture.Height];
            for (int x = 0; x < texture.Width; x++)
                for (int y = 0; y < texture.Height; y++)
                    colors2D[x, y] = colors1D[x + y * texture.Width];

            return colors2D;
        }

        internal static Texture2D GetRandomSplatter()
        {
            return Pixel;
        }

        /// <summary>
        /// Get the map specified by the given name.
        /// </summary>
        /// <param name="name">The name of the map.</param>
        /// <param name="Content">The content manager of the game.</param>
        /// <returns>Return the map specified by the given name.</returns>
        public static Map LoadMap(String name, ContentManager Content)
        {
            return Content.Load<Map>("Maps" + DIR_SEPARATOR + name);
        }
    }
}
