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

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using ModularFI;

namespace Kopernicus
{
    namespace Components
    {
        /// <summary>
        /// Implementation of the <see cref="Sun"/> API.
        /// </summary>
        public class KopernicusStar : Sun
        {
            /// <summary>
            /// A list of all stars
            /// </summary>
            public static List<KopernicusStar> Stars;

            /// <summary>
            /// The results of the latest flux calculation for each star
            /// </summary>
            public static Dictionary<string, double> SolarFlux;

            /// <summary>
            /// The currently active star, for stuff we cant patch
            /// </summary>
            public static KopernicusStar Current;

            /// <summary>
            /// The sunlight
            /// </summary>
            public Light light;

            /// <summary>
            /// The light when we are in IVA
            /// </summary>
            public IVASun iva;

            /// <summary>
            /// The light settings for this star
            /// </summary>
            public LightShifter shifter;

            /// <summary>
            /// Override for <see cref="FlightIntegrator.CalculateSunBodyFlux"/>
            /// </summary>
            public static void SunBodyFlux(ModularFlightIntegrator flightIntegrator)
            {
                // Set Physics
                PhysicsGlobals.SolarLuminosityAtHome = Current.shifter.solarLuminosity;
                PhysicsGlobals.SolarInsolationAtHome = Current.shifter.solarInsolation;
                CalculatePhysics();

                // Get "Correct" values
                flightIntegrator.BaseFICalculateSunBodyFlux();

                // FI Values
                bool directSunlight = flightIntegrator.Vessel.directSunlight;
                double solarFlux = flightIntegrator.solarFlux;
                if (!SolarFlux.ContainsKey(Current.name))
                    SolarFlux.Add(Current.name, solarFlux);
                else
                    SolarFlux[Current.name] = solarFlux;

                // Calculate the values for all bodies
                foreach (KopernicusStar star in Stars.Where(s => s.sun != FlightIntegrator.sunBody))
                {
                    // Set Physics
                    PhysicsGlobals.SolarLuminosityAtHome = star.shifter.solarLuminosity;
                    PhysicsGlobals.SolarInsolationAtHome = star.shifter.solarInsolation;
                    CalculatePhysics();

                    // Calculate Flux
                    Double flux = Flux(flightIntegrator, star);

                    // And save them
                    if (flux > 0)
                        directSunlight = true;
                    solarFlux += flux;
                    if (!SolarFlux.ContainsKey(star.name))
                        SolarFlux.Add(star.name, flux);
                    else
                        SolarFlux[star.name] = flux;
                }

                // Reapply
                flightIntegrator.Vessel.directSunlight = directSunlight;
                flightIntegrator.solarFlux = solarFlux;

                // Set Physics
                PhysicsGlobals.SolarLuminosityAtHome = Current.shifter.solarLuminosity;
                PhysicsGlobals.SolarInsolationAtHome = Current.shifter.solarInsolation;
                CalculatePhysics(); 
            }

            /// <summary>
            /// Fixes the Calculation for Luminosity
            /// NEVER REMOVE THIS AGAIN!
            /// EVEN IF SQUAD MAKES EVERY FIELD PUBLIC AND OPENSOURCE AND WHATNOT
            /// </summary>
            public static void CalculatePhysics()
            {
                if (!FlightGlobals.ready) return;
                CelestialBody homeBody = FlightGlobals.GetHomeBody();
                if (homeBody == null) return;
                while (Stars.All(s => s.sun != homeBody.referenceBody) && homeBody.referenceBody != null)
                    homeBody = homeBody.referenceBody;
                typeof(PhysicsGlobals).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(double)).Skip(2).First().SetValue(PhysicsGlobals.Instance, Math.Pow(homeBody.orbit.semiMajorAxis, 2) * 4 * 3.14159265358979 * PhysicsGlobals.SolarLuminosityAtHome);
            }

            /// <summary>
            /// Small method to handle flux
            /// </summary>
            public static Double Flux(ModularFlightIntegrator fi, KopernicusStar star)
            {
                // Nullchecks
                if (fi.Vessel == null || fi.Vessel.state == Vessel.State.DEAD || fi.CurrentMainBody == null)
                {
                    return 0;
                }

                // Get sunVector
                RaycastHit raycastHit;
                Boolean directSunlight = false;
                Vector3d scaledSpace = ScaledSpace.LocalToScaledSpace(fi.IntegratorTransform.position);
                double scale = Math.Max((star.sun.scaledBody.transform.position - scaledSpace).magnitude, 1);
                Vector3 sunVector = (star.sun.scaledBody.transform.position - scaledSpace) / scale;
                Ray ray = new Ray(ScaledSpace.LocalToScaledSpace(fi.IntegratorTransform.position), sunVector);

                // Get Solar Flux
                double realDistanceToSun = 0;
                if (!Physics.Raycast(ray, out raycastHit, Single.MaxValue, ModularFlightIntegrator.SunLayerMask))
                {
                    directSunlight = true;
                    realDistanceToSun = scale * ScaledSpace.ScaleFactor - star.sun.Radius;
                }
                else if (raycastHit.transform.GetComponent<ScaledMovement>().celestialBody == star.sun)
                {
                    realDistanceToSun = ScaledSpace.ScaleFactor * raycastHit.distance;
                    directSunlight = true;
                }
                if (directSunlight)
                {
                    return PhysicsGlobals.SolarLuminosity/(12.5663706143592*realDistanceToSun*realDistanceToSun);
                }
                return 0;
            }

            /// <summary>
            /// Returns the star the given body orbits
            /// </summary>
            public static KopernicusStar GetNearest(CelestialBody body)
            {
                return Stars.OrderBy(s => Vector3.Distance(body.position, s.sun.position)).First();
            }

            /// <summary>
            /// Starts up fi instance
            /// </summary>
            protected override void Awake()
            {
                if (Stars == null)
                    Stars = new List<KopernicusStar>();
                if (SolarFlux == null)
                    SolarFlux = new Dictionary<string, double>();
                Stars.Add(this);
                DontDestroyOnLoad(this);
                light = gameObject.GetComponent<Light>();

                // Gah
                typeof(Sun).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Last(f => f.FieldType == typeof(Light)).SetValue(this, light);

                // sun flare
                Camera.onPreCull += cam =>
                {
                    Vector3d scaledSpace = target.transform.position - ScaledSpace.LocalToScaledSpace(sun.position);
                    sunDirection = scaledSpace.normalized;
                    if (sunDirection != Vector3d.zero)
                        transform.forward = sunDirection;
                };
            }

            /// <summary>
            /// Create stuff when the object is live
            /// </summary>
            protected override void Start()
            {
                // Get the LightShifter
                shifter = sun.scaledBody.GetComponentsInChildren<LightShifter>(true)?[0];

                // Lensflare
                sunFlare.flare = shifter.sunFlare ?? sunFlare.flare;

                // IVA Light
                if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                {
                    iva = Instantiate(Resources.FindObjectsOfTypeAll<IVASun>().Last());
                    iva.transform.parent = transform;
                    iva.sunT = transform;
                }

                // Scaled Space Light
                if (!useLocalSpaceSunLight) return;
                scaledSunLight = (new GameObject("Scaledspace SunLight " + sun.name)).AddComponent<Light>();
                scaledSunLight.type = LightType.Directional;
                scaledSunLight.intensity = light.intensity;
                scaledSunLight.color = light.color;
                scaledSunLight.transform.parent = transform;
                scaledSunLight.transform.localPosition = Vector3.zero;
                scaledSunLight.transform.localRotation = Quaternion.identity;
                scaledSunLight.cullingMask = 1 << 10;
                GameEvents.onGameSceneLoadRequested.Add(SceneLoaded);
            }

            /// <summary>
            /// Updates the light values based on the current scene
            /// </summary>
            /// <param name="scene"></param>
            void SceneLoaded(GameScenes scene)
            {
                light.shadowBias = scene != GameScenes.SPACECENTER ? 0.125f : 1f;
                if (gameObject.GetComponentInChildren<IVASun>() != null)
                    DestroyImmediate(gameObject.GetComponentInChildren<IVASun>().gameObject);
            }

            /// <summary>
            /// Updates this instance
            /// </summary>
            void LateUpdate()
            {
                // Apply light settings
                if (light)
                {
                    light.color = shifter.sunlightColor;
                    light.intensity = shifter.intensityCurve.Evaluate((float)Vector3d.Distance(sun.position, target.position));
                    light.shadowStrength = shifter.sunlightShadowStrength;
                }

                // Patch the ScaledSpace light
                if (scaledSunLight)
                {
                    scaledSunLight.color = shifter.scaledSunlightColor;
                    scaledSunLight.intensity = shifter.scaledIntensityCurve.Evaluate((float)Vector3d.Distance(ScaledSpace.LocalToScaledSpace(sun.position), target.position));
                }

                if (HighLogic.LoadedSceneIsFlight && iva?.GetComponent<Light>())
                {
                    iva.GetComponent<Light>().color = shifter.IVASunColor;
                    iva.GetComponent<Light>().intensity = shifter.IVASunIntensity;
                }

                // Set SunFlare color
                sunFlare.color = shifter.sunLensFlareColor;

                // Set other stuff
                AU = shifter.AU;
                brightnessCurve = shifter.brightnessCurve.Curve;

                // Update the lensflare orientation and scale
                sunFlare.brightness = brightnessMultiplier * brightnessCurve.Evaluate((float)(1 / (Vector3d.Distance(target.position, ScaledSpace.LocalToScaledSpace(sun.position)) / (AU * ScaledSpace.InverseScaleFactor))));

                // States
                bool lightsOn = (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium || HighLogic.LoadedScene == GameScenes.SPACECENTER);
                light.enabled = shifter.givesOffLight && lightsOn;
                sunFlare.enabled = shifter.givesOffLight && lightsOn;
                if (useLocalSpaceSunLight && Sun.Instance.useLocalSpaceSunLight)
                    scaledSunLight.enabled = shifter.givesOffLight && lightsOn;

                // Update Scaled Space Light
                if (!useLocalSpaceSunLight) return;
                Vector3d localSpace = ScaledSpace.ScaledToLocalSpace(target.position);
                if (FlightGlobals.currentMainBody == null || FlightGlobals.currentMainBody == sun)
                {
                    localTime = 1f;
                    light.intensity = scaledSunLight.intensity;
                }
                else
                {
                    double targetAltitude = FlightGlobals.getAltitudeAtPos(localSpace, FlightGlobals.currentMainBody);
                    if (targetAltitude < 0)
                        targetAltitude = 0;
                    double horizonAngle = Math.Acos(FlightGlobals.currentMainBody.Radius / (FlightGlobals.currentMainBody.Radius + targetAltitude));
                    float horizonScalar = -Mathf.Sin((float)horizonAngle);
                    float dayNightRatio = 1f - Mathf.Abs(horizonScalar);
                    float fadeStartAtAlt = horizonScalar + fadeStart * dayNightRatio;
                    float fadeEndAtAlt = horizonScalar - fadeEnd * dayNightRatio;
                    localTime = Vector3.Dot(-FlightGlobals.getUpAxis(localSpace), transform.forward);
                    light.intensity = Mathf.Lerp(0f, scaledSunLight.intensity, Mathf.InverseLerp(fadeEndAtAlt, fadeStartAtAlt, localTime));
                }
            }

            /// <summary>
            /// Override this function and use <see cref="Current"/> instead of Planetarium sun
            /// </summary>
            public override double GetLocalTimeAtPosition(Vector3d wPos, CelestialBody cb)
            {
                Vector3d pos1 = Vector3d.Exclude(cb.angularVelocity, FlightGlobals.getUpAxis(cb, wPos));
                Vector3d pos2 = Vector3d.Exclude(cb.angularVelocity, Current.sun.position - cb.position);
#pragma warning disable CS0618
                double angle = (Vector3d.Dot(Vector3d.Cross(pos2, pos1), cb.angularVelocity) < 0 ? -1 : 1) * Vector3d.AngleBetween(pos1, pos2) / 6.28318530717959 + 0.5;
#pragma warning restore CS0618
                if (angle > Math.PI * 2)
                    angle -= Math.PI * 2;
                return angle;
            }
        }
    }
}