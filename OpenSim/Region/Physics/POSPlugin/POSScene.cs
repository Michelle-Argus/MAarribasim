/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Physics.Manager;

namespace OpenSim.Region.Physics.POSPlugin
{
    public class POSScene : PhysicsScene
    {
        private List<POSCharacter> _characters = new List<POSCharacter>();
        private List<POSPrim> _prims = new List<POSPrim>();
        private float[] _heightMap;
        private const float gravity = -9.8f;

        public POSScene()
        {
        }

        public override void Initialise(IMesher meshmerizer, IConfigSource config)
        {
        }

        public override void Dispose()
        {
        }

        public override PhysicsActor AddAvatar(string avName, PhysicsVector position, PhysicsVector size)
        {
            POSCharacter act = new POSCharacter();
            act.Position = position;
            _characters.Add(act);
            return act;
        }

        public override void SetWaterLevel(float baseheight)
        {
        }

        public override void RemovePrim(PhysicsActor prim)
        {
            POSPrim p = (POSPrim) prim;
            if (_prims.Contains(p))
            {
                _prims.Remove(p);
            }
        }

        public override void RemoveAvatar(PhysicsActor character)
        {
            POSCharacter act = (POSCharacter) character;
            if (_characters.Contains(act))
            {
                _characters.Remove(act);
            }
        }

/*
        public override PhysicsActor AddPrim(PhysicsVector position, PhysicsVector size, Quaternion rotation)
        {
            return null;
        }
*/

        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, Quaternion rotation)
        {
            return AddPrimShape(primName, pbs, position, size, rotation, false);
        }

        public override PhysicsActor AddPrimShape(string primName, PrimitiveBaseShape pbs, PhysicsVector position,
                                                  PhysicsVector size, Quaternion rotation, bool isPhysical)
        {
            POSPrim prim = new POSPrim();
            prim.Position = position;
            prim.Orientation = rotation;
            prim.Size = size;
            _prims.Add(prim);
            return prim;
        }

        private bool isColliding(POSCharacter c, POSPrim p)
        {
            Vector3 rotatedPos = new Vector3(c.Position.X - p.Position.X, c.Position.Y - p.Position.Y,
                                             c.Position.Z - p.Position.Z) * Quaternion.Inverse(p.Orientation);
            Vector3 avatarSize = new Vector3(c.Size.X, c.Size.Y, c.Size.Z) * Quaternion.Inverse(p.Orientation);

            if (Math.Abs(rotatedPos.X) >= (p.Size.X*0.5 + Math.Abs(avatarSize.X)) ||
                Math.Abs(rotatedPos.Y) >= (p.Size.Y*0.5 + Math.Abs(avatarSize.Y)) ||
                Math.Abs(rotatedPos.Z) >= (p.Size.Z*0.5 + Math.Abs(avatarSize.Z)))
            {
                return false;
            }
            return true;
        }

        private bool isCollidingWithPrim(POSCharacter c)
        {
            for (int i = 0; i < _prims.Count; ++i)
            {
                if (isColliding(c, _prims[i]))
                {
                    return true;
                }
            }

            return false;
        }

        public override void AddPhysicsActorTaint(PhysicsActor prim)
        {
        }

        public override float Simulate(float timeStep)
        {
            float fps = 0;
            for (int i = 0; i < _characters.Count; ++i)
            {
                fps++;
                POSCharacter character = _characters[i];

                float oldposX = character.Position.X;
                float oldposY = character.Position.Y;
                float oldposZ = character.Position.Z;

                if (!character.Flying)
                {
                    character._target_velocity.Z += gravity * timeStep;
                }

                character.Position.X += character._target_velocity.X * timeStep;
                character.Position.Y += character._target_velocity.Y * timeStep;

                character.Position.X = Util.Clamp(character.Position.X, 0.01f, Constants.RegionSize - 0.01f);
                character.Position.Y = Util.Clamp(character.Position.Y, 0.01f, Constants.RegionSize - 0.01f);

                bool forcedZ = false;

                float terrainheight = _heightMap[(int)character.Position.Y * Constants.RegionSize + (int)character.Position.X];
                if (character.Position.Z + (character._target_velocity.Z * timeStep) < terrainheight + 2)
                {
                    character.Position.Z = terrainheight + 1.0f;
                    forcedZ = true;
                }
                else
                {
                    character.Position.Z += character._target_velocity.Z*timeStep;
                }

                /// this is it -- the magic you've all been waiting for!  Ladies and gentlemen --
                /// Completely Bogus Collision Detection!!!
                /// better known as the CBCD algorithm

                if (isCollidingWithPrim(character))
                {
                    character.Position.Z = oldposZ; //  first try Z axis
                    if (isCollidingWithPrim(character))
                    {
                        character.Position.Z = oldposZ + 0.4f; // try harder
                        if (isCollidingWithPrim(character))
                        {
                            character.Position.X = oldposX;
                            character.Position.Y = oldposY;
                            character.Position.Z = oldposZ;

                            character.Position.X += character._target_velocity.X * timeStep;
                            if (isCollidingWithPrim(character))
                            {
                                character.Position.X = oldposX;
                            }

                            character.Position.Y += character._target_velocity.Y * timeStep;
                            if (isCollidingWithPrim(character))
                            {
                                character.Position.Y = oldposY;
                            }
                        }
                        else
                        {
                            forcedZ = true;
                        }
                    }
                    else
                    {
                        forcedZ = true;
                    }
                }

                character.Position.X = Util.Clamp(character.Position.X, 0.01f, Constants.RegionSize - 0.01f);
                character.Position.Y = Util.Clamp(character.Position.Y, 0.01f, Constants.RegionSize - 0.01f);

                character._velocity.X = (character.Position.X - oldposX)/timeStep;
                character._velocity.Y = (character.Position.Y - oldposY)/timeStep;

                if (forcedZ)
                {
                    character._velocity.Z = 0;
                    character._target_velocity.Z = 0;
                    ((PhysicsActor)character).IsColliding = true;
                    character.RequestPhysicsterseUpdate();
                }
                else
                {
                    ((PhysicsActor)character).IsColliding = false;
                    character._velocity.Z = (character.Position.Z - oldposZ)/timeStep;
                }
            }
            return fps;
        }

        public override void GetResults()
        {
        }

        public override bool IsThreaded
        {
            // for now we won't be multithreaded
            get { return (false); }
        }

        public override void SetTerrain(float[] heightMap)
        {
            _heightMap = heightMap;
        }

        public override void DeleteTerrain()
        {
        }

        public override Dictionary<uint, float> GetTopColliders()
        {
            Dictionary<uint, float> returncolliders = new Dictionary<uint, float>();
            return returncolliders;
        }
    }
}
