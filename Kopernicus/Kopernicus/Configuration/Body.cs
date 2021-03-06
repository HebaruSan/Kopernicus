/**
 * Kopernicus Planetary System Modifier
 * ====================================
 * Created by: BryceSchroeder and Teknoman117 (aka. Nathaniel R. Lewis)
 * Maintained by: Thomas P., NathanKell and KillAshley
 * Additional Content by: Gravitasi, aftokino, KCreator, Padishar, Kragrathea, OvenProofMars, zengei, MrHappyFace, Sigma88
 * ------------------------------------------------------------- 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston,
 * MA 02110-1301  USA
 * 
 * This library is intended to be used as a plugin for Kerbal Space Program
 * which is copyright 2011-2015 Squad. Your usage of Kerbal Space Program
 * itself is governed by the terms of its EULA, not the license above.
 * 
 * https://kerbalspaceprogram.com
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Kopernicus.Components;

namespace Kopernicus
{
    namespace Configuration 
    {
        [RequireConfigType(ConfigType.Node)]
        public class Body : IParserEventSubscriber
        {
            // Path of the ScaledSpaceCache (will eventually not matter much)
            public const string ScaledSpaceCacheDirectory = "GameData/Kopernicus/Cache";

            // Body we are trying to edit
            public PSystemBody generatedBody { get; set; }

            // Name of this body
            [PreApply]
            [ParserTarget("name", optional = false)]
            public string name { get; set; }

            [ParserTarget("cacheFile")]
            public string cacheFile { get; set; }

            [ParserTarget("barycenter")]
            public NumericParser<bool> barycenter = new NumericParser<bool>(false);

            [ParserTarget("cbNameLater")]
            public string cbNameLater
            {
                get
                {
                    if (generatedBody.celestialBody.GetComponent<NameChanger>())
                        return generatedBody.celestialBody.GetComponent<NameChanger>().newName;
                    return "";
                }
                set
                {
                    if (!generatedBody.celestialBody.GetComponent<NameChanger>())
                    {
                        NameChanger changer = generatedBody.celestialBody.gameObject.AddComponent<NameChanger>();
                        changer.oldName = name;
                        changer.newName = value;
                    }
                    else
                        generatedBody.celestialBody.gameObject.GetComponent<NameChanger>().newName = cbNameLater;
                }
            }
            
            // Flight globals index of this body - for computing reference id
            [ParserTarget("flightGlobalsIndex")]
            public NumericParser<int> flightGlobalsIndex 
            {
                get { return generatedBody.flightGlobalsIndex; }
                set { generatedBody.flightGlobalsIndex = value.value; }
            }

            // Finalize the orbit of the body?
            [ParserTarget("finalizeOrbit")]
            public NumericParser<bool> finalizeOrbit
            {
                get { return generatedBody.Has("finalizeBody"); }
                set { if (value) generatedBody.Set("finalizeBody", true); }
            }

            // Whether this body should be taken into account for the main menu body stuff
            [ParserTarget("randomMainMenuBody")]
            public NumericParser<bool> canBeMainMenuBody
            {
                get { return Loader.Instance?.randomMainMenuBodies.Contains(name); }
                set { if (value) Loader.Instance.randomMainMenuBodies.Add(name); }
            }

            // Template property of a body - responsible for generating a PSystemBody from an existing one
            [PreApply]
            [ParserTarget("Template")]
            public TemplateLoader template { get; set; }

            // Celestial body properties (description, mass, etc.)
            [ParserTarget("Properties", allowMerge = true)]
            public PropertiesLoader properties { get; set; }

            // Wrapper around KSP's Orbit class for editing/loading
            [ParserTarget("Orbit", allowMerge = true)]
            public OrbitLoader orbit { get; set; }

            // Wrapper around the settings for the world's scaled version
            [ParserTarget("ScaledVersion", allowMerge = true)]
            public ScaledVersionLoader scaledVersion { get; set; }
            
            // Wrapper around the settings for the world's atmosphere
            [ParserTarget("Atmosphere", allowMerge = true)]
            public AtmosphereLoader atmosphere { get; set; }

            // Wrapper around the settings for the PQS
            [ParserTarget("PQS", allowMerge = true)]
            public PQSLoader pqs { get; set; }

            // Wrapper around the settings for the Ocean
            [ParserTarget("Ocean", allowMerge = true)]
            public OceanLoader ocean { get; set; }

            // Wrapper around Ring class for editing/loading
            [ParserTargetCollection("Rings", nameSignificance = NameSignificance.None, allowMerge = true)]
            public List<RingLoader> rings = new List<RingLoader>();

            // Wrapper around Particle class for editing/loading
            [ParserTargetCollection("Particles", nameSignificance = NameSignificance.None, allowMerge = true)]
            public List<ParticleLoader> particle = new List<ParticleLoader>();
        
            // Wrapper around the settings for the SpaceCenter
            [ParserTarget("SpaceCenter", allowMerge = true)]
            public SpaceCenterLoader spaceCenter { get; set; }

            // Wrapper around DebugMode settings
            [ParserTarget("Debug")]
            public DebugLoader debug { get; set; }

            // Post spawn orbit patcher
            [ParserTarget("PostSpawnOrbit")]
            public ConfigNode postspawn
            {
                set { generatedBody.Set("orbitPatches", value); }
            }

            // Parser Apply Event
            void IParserEventSubscriber.Apply(ConfigNode node)
            {
                // If we have a template, generatedBody *is* the template body
                if (template != null && template.body)
                {
                    generatedBody = template.body;

                    // Patch the game object names in the template
                    generatedBody.name = name;
                    generatedBody.celestialBody.bodyName = name;
                    generatedBody.celestialBody.transform.name = name;
                    generatedBody.celestialBody.bodyTransform.name = name;
                    generatedBody.scaledVersion.name = name;
                    if (generatedBody.pqsVersion != null)
                    {
                        generatedBody.pqsVersion.name = name;
                        generatedBody.pqsVersion.gameObject.name = name;
                        generatedBody.pqsVersion.transform.name = name;
                        foreach (PQS p in generatedBody.pqsVersion.GetComponentsInChildren<PQS>(true))
                            p.name = p.name.Replace(template.body.celestialBody.bodyName, name);
                    }

                    // If we've changed the name, reset use_The_InName
                    if (generatedBody.name != template.originalBody.celestialBody.bodyName)
                    {
                        generatedBody.celestialBody.bodyDisplayName = generatedBody.celestialBody.bodyName;
                    }
                }

                // Otherwise we have to generate all the things for this body
                else
                {
                    // Create the PSystemBody object
                    GameObject generatedBodyGameObject = new GameObject(name);
                    generatedBodyGameObject.transform.parent = Utility.Deactivator;
                    generatedBody = generatedBodyGameObject.AddComponent<PSystemBody>();
                    generatedBody.flightGlobalsIndex = 0;

                    // Create the celestial body
                    GameObject generatedBodyProperties = new GameObject(name);
                    generatedBodyProperties.transform.parent = generatedBodyGameObject.transform;
                    generatedBody.celestialBody = generatedBodyProperties.AddComponent<CelestialBody>();
                    generatedBody.celestialBody.progressTree = null;

                    // Sensible defaults 
                    generatedBody.celestialBody.bodyName = name;
                    generatedBody.celestialBody.bodyDisplayName = name;
                    generatedBody.celestialBody.atmosphere = false;
                    generatedBody.celestialBody.ocean = false;

                    // Create the scaled version
                    generatedBody.scaledVersion = new GameObject(name);
                    generatedBody.scaledVersion.layer = Constants.GameLayers.ScaledSpace;
                    generatedBody.scaledVersion.transform.parent = Utility.Deactivator;
                }
            }

            // Parser Post Apply Event
            void IParserEventSubscriber.PostApply(ConfigNode node)
            {
                // If Debug Mode is null, create default values
                if (debug == null) debug = new DebugLoader();
                if (scaledVersion == null) scaledVersion = new ScaledVersionLoader();

                // PQS
                if (generatedBody.pqsVersion)
                {
                    // Adjust the radius of the PQSs appropriately
                    foreach (PQS p in generatedBody.pqsVersion.GetComponentsInChildren<PQS>(true))
                        p.radius = generatedBody.celestialBody.Radius;
                }

                // Create a barycenter
                if (barycenter.value)
                {
                    // Register the body for post-spawn patching
                    generatedBody.Set("barycenter", true);

                    // Nuke the PQS
                    if (generatedBody.pqsVersion != null)
                    {
                        generatedBody.pqsVersion.transform.parent = null;
                        UnityEngine.Object.Destroy(generatedBody.pqsVersion);
                        generatedBody.pqsVersion = null;
                    }

                    // Stop ScaledSpace Cache
                    scaledVersion.deferMesh = true;
                }

                // We need to generate new scaled space meshes if 
                //   a) we are using a template and we've change either the radius or type of body
                //   b) we aren't using a template
                //   c) debug mode is active
                if (!scaledVersion.deferMesh &&
                    (((template != null) && (Math.Abs(template.radius - generatedBody.celestialBody.Radius) > 1.0 || template.type != scaledVersion.type.value))
                    || template == null || debug.update))
                {

                    Utility.UpdateScaledMesh(generatedBody.scaledVersion,
                                                generatedBody.pqsVersion,
                                                generatedBody.celestialBody,
                                                ScaledSpaceCacheDirectory,
                                                cacheFile,
                                                debug.exportMesh,
                                                scaledVersion.sphericalModel);
                }

                // Visualize the SOI
                if (debug.showSOI)
                    generatedBody.celestialBody.gameObject.AddComponent<Wiresphere>();

                // Loads external parser targets
                Parser.LoadParserTargetsExternal(node, "Kopernicus", configName: "Kopernicus");

                // Post gen celestial body
                Utility.DumpObjectFields(generatedBody.celestialBody, " Celestial Body ");
            }
        }
    }
}
