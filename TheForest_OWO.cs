using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MelonLoader;
using HarmonyLib;
using UnityEngine;

using MyOwoVest;

namespace TheForest_bhaptics
{
    public class TheForest_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;
        public static bool isRightHanded = true;
        public static bool planeFallen = false;

        public override void OnInitializeMelon()
        {
            //base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            //tactsuitVr.PlayBackFeedback("HeartBeat");
        }

        #region General updates

        private static KeyValuePair<float, float> getAngleAndShift(Transform player, Vector3 hit)
        {
            // bhaptics starts in the front, then rotates to the left. 0° is front, 90° is left, 270° is right.
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            // y is "up", z is "forward" in local coordinates
            Vector3 hitPosition = hit - player.position;
            Quaternion myPlayerRotation = player.rotation;
            Vector3 playerDir = myPlayerRotation.eulerAngles;
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);
            float earlyhitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            Vector3 earlycrossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (earlycrossProduct.y > 0f) { earlyhitAngle *= -1f; }
            float myRotation = earlyhitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) { myRotation = 360f + myRotation; }


            float hitShift = hitPosition.y;
            float upperBound = -1.0f;
            float lowerBound = -2.0f;
            if (hitShift > upperBound) { hitShift = 0.5f; }
            else if (hitShift < lowerBound) { hitShift = -0.5f; }
            else { hitShift = (hitShift - lowerBound) / (upperBound - lowerBound) - 0.5f; }

            return new KeyValuePair<float, float>(myRotation, hitShift);
        }

        [HarmonyPatch(typeof(VRPlayerControl), "UpdateWeaponHandedness", new Type[] { })]
        public class bhaptics_UpdateHandedness
        {
            [HarmonyPostfix]
            public static void Postfix(VRPlayerControl __instance)
            {
                isRightHanded = !__instance.RightHandedActive;
                //tactsuitVr.LOG("Right hand: " + isRightHanded.ToString());
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "HealthChange", new Type[] { typeof(float) })]
        public class bhaptics_HealthChange
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerStats __instance, float amount)
            {
                if (__instance.IsHealthInGreyZone) { tactsuitVr.PlayBackFeedback("ThreeHeartBeats"); }
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "CheckStats", new Type[] {  })]
        public class bhaptics_CheckStats
        {
            [HarmonyPostfix]
            public static void Postfix(PlayerStats __instance)
            {
                if (__instance.IsCold) { tactsuitVr.PlayBackFeedback("Shiver"); }
            }
        }

        #endregion

        #region Eating / drinking

        [HarmonyPatch(typeof(PlayerStats), "Drink", new Type[] { })]
        public class bhaptics_PlayerDrinks
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Drinking");
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "DrinkBooze", new Type[] { })]
        public class bhaptics_PlayerDrinksBooze
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Drinking");
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "DrinkLake", new Type[] { })]
        public class bhaptics_PlayerDrinksLake
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Drinking");
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayEat", new Type[] { })]
        public class bhaptics_PlayEat
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Eating");
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayDrink", new Type[] { })]
        public class bhaptics_PlayDrink
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Drinking");
            }
        }


        [HarmonyPatch(typeof(PlayerSfx), "PlayStaminaBreath", new Type[] { })]
        public class bhaptics_PlayStaminaBreath
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Breathing");
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayColdSfx", new Type[] { })]
        public class bhaptics_PlayCold
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Shiver");
            }
        }

        
        #endregion

        #region World interaction

        [HarmonyPatch(typeof(WaterViz), "Update", new Type[] {  })]
        public class bhaptics_WaterViz
        {
            [HarmonyPostfix]
            public static void Postfix(WaterViz __instance)
            {
                if (__instance.InWater) { tactsuitVr.PlayBackFeedback("Diving"); }
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayOpenInventory", new Type[] { })]
        public class bhaptics_OpenInventory
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("GetBackpack");
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayCloseInventory", new Type[] { })]
        public class bhaptics_CloseInventory
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("PutAwayBackpack");
            }
        }

        [HarmonyPatch(typeof(RainSfx), "OnEnable", new Type[] { })]
        public class bhaptics_StartRain
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StartRain();
            }
        }

        [HarmonyPatch(typeof(RainSfx), "OnDisable", new Type[] { })]
        public class bhaptics_StopRain
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopRain();
            }
        }

        [HarmonyPatch(typeof(TheForest.World.WeatherSystem), "Lightning", new Type[] { })]
        public class bhaptics_Lightning
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("Thunder");
            }
        }

        [HarmonyPatch(typeof(FirstPersonCharacter), "OnCollisionEnterProxied", new Type[] { typeof(Collision) })]
        public class bhaptics_CollisionEnter
        {
            [HarmonyPostfix]
            public static void Postfix(FirstPersonCharacter __instance, Collision coll)
            {
                if (coll.impulse.y >= 100f) { tactsuitVr.PlayBackFeedback("FallDamage"); return; }
                //tactsuitVr.LOG("Collision vector: " + coll.impulse.x.ToString() + " " + coll.impulse.y.ToString() + " " + coll.impulse.z.ToString());
                //tactsuitVr.LOG("Collision impulse: " + coll.impulse.magnitude.ToString());
                //tactsuitVr.LOG("Character rotation: " + __instance.transform.rotation.eulerAngles.y.ToString());
                string colliderName = coll.collider.name;
                string[] unimportant_Colliders = { "ground", "Collision", "Collsion_Cube", "MainTerrain" };
                Transform myPlayer = __instance.transform;
                float intensity = Math.Min(coll.impulse.magnitude / 100f, 1f);
                foreach (ContactPoint point in coll.contacts )
                {
                    Vector3 myHit = point.point - myPlayer.position;
                    if (myHit.y >= 0f) { continue; }
                    if (myHit.y <= -2.0f)
                    {
                        // player bumped into something with their feet.
                        // (fall damage is already done, so) for nonessential stuff, just skip feedback
                        if (unimportant_Colliders.Any(colliderName.Contains))
                        {
                            // tactsuitVr.LOG("Ignored collider: " + colliderName);
                            continue;
                        }
                        // even for important item, very low down only at lowered intensity
                        intensity *= 0.7f;
                    }
                    //tactsuitVr.LOG("Contact point: " + point.point.x.ToString() + " " + point.point.y.ToString() + " " + point.point.z.ToString());
                    //tactsuitVr.LOG("Hit point: " + myHit.x.ToString() + " " + myHit.y.ToString() + " " + myHit.z.ToString());
                    var angleShift = getAngleAndShift(myPlayer, point.point);
                    tactsuitVr.PlayBackHit("Impact", angleShift.Key, angleShift.Value, intensity);
                }
                //tactsuitVr.LOG(" ");
            }
        }
        

        #endregion

        #region Take damage

        [HarmonyPatch(typeof(PlayerStats), "Explosion", new Type[] { typeof(float) })]
        public class bhaptics_Explosion
        {
            [HarmonyPostfix]
            public static void Postfix(float getDist)
            {
                float intensity = Math.Max((50f - getDist) / 50f, 0.0f);
                tactsuitVr.PlayBackFeedback("Explosion", intensity);
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "Explosion", new Type[] { typeof(float), typeof(bool) })]
        public class bhaptics_ExplosionTwo
        {
            [HarmonyPostfix]
            public static void Postfix(float dist, bool fromPlayer)
            {
                float intensity = Math.Max((50f - dist) / 50f, 0.0f);
                tactsuitVr.PlayBackFeedback("Explosion", intensity);
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "ExplosionPlayer", new Type[] { typeof(float) })]
        public class bhaptics_ExplosionPlayer
        {
            [HarmonyPostfix]
            public static void Postfix(float hitDist)
            {
                float intensity = Math.Max((50f - hitDist) / 50f, 0.0f);
                tactsuitVr.PlayBackFeedback("Explosion", intensity);
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "Fell", new Type[] {  })]
        public class bhaptics_PlayerFell
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                //tactsuitVr.LOG("Fell");
                tactsuitVr.PlayBackFeedback("FallDamage");
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "Hit", new Type[] { typeof(int), typeof(bool), typeof(PlayerStats.DamageType) })]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(int damage, bool ignoreArmor, PlayerStats.DamageType type)
            {
                //tactsuitVr.LOG("Damage: " + type.ToString());
                switch (type)
                {
                    case PlayerStats.DamageType.Drowning:
                        tactsuitVr.PlayBackFeedback("Choking");
                        break;
                    case PlayerStats.DamageType.Fire:
                        tactsuitVr.PlayBackFeedback("Burning");
                        break;
                    case PlayerStats.DamageType.Frost:
                        tactsuitVr.PlayBackFeedback("Freezing");
                        break;
                    case PlayerStats.DamageType.Poison:
                        tactsuitVr.PlayBackFeedback("Poison");
                        break;
                    case PlayerStats.DamageType.Physical:
                        tactsuitVr.PlayBackFeedback("HitFront");
                        break;
                    default:
                        break;
                }
            }
        }

        /*
        [HarmonyPatch(typeof(PlayerStats), "getHitDirection", new Type[] { typeof(Vector3) })]
        public class bhaptics_getHitDirection
        {
            [HarmonyPostfix]
            public static void Postfix(Vector3 pos)
            {
                tactsuitVr.LOG("Hit direction: " + pos.x.ToString() + " " + pos.z.ToString());
            }
        }
        

        [HarmonyPatch(typeof(PlayerStats), "hitFallDown", new Type[] {  })]
        public class bhaptics_HitFallDown
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.LOG("HitFallDown");
                tactsuitVr.PlayBackFeedback("FallDamage");
            }
        }
        */
        #endregion

        #region Weapons

        [HarmonyPatch(typeof(weaponInfo), "OnTriggerEnter", new Type[] { typeof(Collider) })]
        public class bhaptics_TriggerEnter
        {
            [HarmonyPostfix]
            public static void Postfix(weaponInfo __instance, Collider other)
            {
                tactsuitVr.Recoil(isRightHanded);
            }
        }

        [HarmonyPatch(typeof(GunShoot), "Start", new Type[] {  })]
        public class bhaptics_GunShoot
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.Recoil(isRightHanded);
            }
        }

        [HarmonyPatch(typeof(PlayerSfx), "PlayShootFlareSfx", new Type[] { })]
        public class bhaptics_FlareShoot
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.Recoil(isRightHanded);
            }
        }

        #endregion

        #region Die / Knockout

        [HarmonyPatch(typeof(PlayerStats), "KillPlayer", new Type[] {  })]
        public class bhaptics_KillPlayer
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(PlayerStats), "KnockOut", new Type[] { })]
        public class bhaptics_KnockOut
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
                tactsuitVr.PlayBackFeedback("ThreeHeartBeats");
            }
        }
        
        [HarmonyPatch(typeof(PlayerStats), "WakeFromKnockOut", new Type[] { typeof(bool), typeof(WaitForSeconds) })]
        public class bhaptics_WakeFromKnockOut
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("NeckTingleShort");
            }
        }

        #endregion

        #region Plane Crash

        [HarmonyPatch(typeof(planeEvents), "fallForward2", new Type[] { })]
        public class bhaptics_PlaneFallForward2
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                //tactsuitVr.LOG("Fall2");
                tactsuitVr.PlayBackFeedback("PlaneHitGround");
            }
        }

        [HarmonyPatch(typeof(planeEvents), "hitGround", new Type[] { })]
        public class bhaptics_PlaneHitGround
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("PlaneHitGround");
            }
        }

        [HarmonyPatch(typeof(planeEvents), "goBlack", new Type[] { })]
        public class bhaptics_PlaneGoBlack
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlayBackFeedback("ThreeHeartBeats");
            }
        }
        #endregion

    }
}
