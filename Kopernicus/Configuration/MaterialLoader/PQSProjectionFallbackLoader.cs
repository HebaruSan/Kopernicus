// Material wrapper generated by shader translator tool
using System;
using System.Reflection;
using UnityEngine;

using Kopernicus.MaterialWrapper;

namespace Kopernicus
{
    namespace Configuration
    {
        public class PQSProjectionFallbackLoader : PQSProjectionFallback
        {
            // Saturation, default = 1
            [ParserTarget("saturation", optional = true)]
            public NumericParser<float> saturationSetter
            {
                get { return saturation; }
                set { saturation = value; }
            }

            // Contrast, default = 1
            [ParserTarget("contrast", optional = true)]
            public NumericParser<float> contrastSetter
            {
                get { return contrast; }
                set { contrast = value; }
            }

            // Colour Unsaturation (A = Factor), default = (1,1,1,0)
            [ParserTarget("tintColor", optional = true)]
            public ColorParser tintColorSetter
            {
                get { return tintColor; }
                set { tintColor = value; }
            }

            // Near Tiling, default = 1000
            [ParserTarget("texTiling", optional = true)]
            public NumericParser<float> texTilingSetter
            {
                get { return texTiling; }
                set { texTiling = value; }
            }

            // Near Blend, default = 0.5
            [ParserTarget("texPower", optional = true)]
            public NumericParser<float> texPowerSetter
            {
                get { return texPower; }
                set { texPower = value; }
            }

            // Far Blend, default = 0.5
            [ParserTarget("multiPower", optional = true)]
            public NumericParser<float> multiPowerSetter
            {
                get { return multiPower; }
                set { multiPower = value; }
            }

            // NearFar Start, default = 2000
            [ParserTarget("groundTexStart", optional = true)]
            public NumericParser<float> groundTexStartSetter
            {
                get { return groundTexStart; }
                set { groundTexStart = value; }
            }

            // NearFar Start, default = 10000
            [ParserTarget("groundTexEnd", optional = true)]
            public NumericParser<float> groundTexEndSetter
            {
                get { return groundTexEnd; }
                set { groundTexEnd = value; }
            }

            // Multifactor, default = 0.5
            [ParserTarget("multiFactor", optional = true)]
            public NumericParser<float> multiFactorSetter
            {
                get { return multiFactor; }
                set { multiFactor = value; }
            }

            // Main Texture, default = "white" {}
            [ParserTarget("mainTex", optional = true)]
            public Texture2DParser mainTexSetter
            {
                get { return mainTex; }
                set { mainTex = value; }
            }

            [ParserTarget("mainTexScale", optional = true)]
            public Vector2Parser mainTexScaleSetter
            {
                get { return mainTexScale; }
                set { mainTexScale = value; }
            }

            [ParserTarget("mainTexOffset", optional = true)]
            public Vector2Parser mainTexOffsetSetter
            {
                get { return mainTexOffset; }
                set { mainTexOffset = value; }
            }

            // PlanetOpacity, default = 1
            [ParserTarget("planetOpacity", optional = true)]
            public NumericParser<float> planetOpacitySetter
            {
                get { return planetOpacity; }
                set { planetOpacity = value; }
            }

            // Constructors
            public PQSProjectionFallbackLoader () : base() { }
            public PQSProjectionFallbackLoader (string contents) : base (contents) { }
            public PQSProjectionFallbackLoader (Material material) : base(material) { }
        }
    }
}
