using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Helpers
{
    public static class UnityHelpers
    {
        public static Vector3 ToUnityVector3(this SerializableVector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static SerializableVector3 ToSerializableVector3(this Vector3 vector)
        {
            return new SerializableVector3(vector.x, vector.y, vector.z);
        }

        public static IEnumerable<Vector3> AreWithinHorizonalLimitsOf(this IEnumerable<Vector3> testVectors, Vector3 referenceVector, float maxHorizontalDegrees)
        {
            Vector3 oppositeFromWallNormalized = Vector3.ProjectOnPlane(-referenceVector, Vector3.up).normalized;

            float minDotProduct = Mathf.Cos(maxHorizontalDegrees * Mathf.Deg2Rad);

            foreach (Vector3 testVector in testVectors)
            {
                Vector3 directionOnXZ = Vector3.ProjectOnPlane(testVector, Vector3.up);

                // Ignore locations within ~5 deg of vertical
                if (directionOnXZ.sqrMagnitude < 0.1f)
                {
                    continue;
                }

                float dotProduct = Vector3.Dot(oppositeFromWallNormalized, directionOnXZ.normalized);
                if (dotProduct >= minDotProduct)
                {
                    yield return testVector;
                }
            }
        }

        public static IEnumerable<Vector3> ClampToBeWithin(this IEnumerable<Vector3> vectors, Vector3 referenceVector, float maxHorizontalDegrees, float maxVerticalDegreesDown, float maxVerticalDegreesUp)
        {
            Quaternion referenceRotation = Quaternion.LookRotation(referenceVector.normalized, Vector3.up);
            Quaternion inverseReferenceRotation = Quaternion.Inverse(referenceRotation);

            foreach (Vector3 vector in vectors)
            {
                Vector3 vectorLocal = inverseReferenceRotation * vector.normalized;

                float yaw = Mathf.Atan2(vectorLocal.x, vectorLocal.z) * Mathf.Rad2Deg;
                float pitch = Mathf.Atan2(vectorLocal.y, Mathf.Sqrt(vectorLocal.x * vectorLocal.x + vectorLocal.z * vectorLocal.z)) * Mathf.Rad2Deg;

                yaw = Mathf.Clamp(yaw, -1 * maxHorizontalDegrees, maxHorizontalDegrees);
                pitch = Mathf.Clamp(pitch, -1 * maxVerticalDegreesDown, maxVerticalDegreesUp);

                Quaternion clampedRotation = Quaternion.Euler(pitch, yaw, 0f);

                Vector3 clampedVector = (referenceRotation * clampedRotation * Vector3.forward).normalized;
                yield return clampedVector;
            }
        }

        public static IEnumerable<Vector3> ApplyRandomOffsets(this IEnumerable<Vector3> vectors, float maxHorizontalDegrees, float maxVerticalDegreesDown, float maxVerticalDegreesUp)
        {
            foreach (Vector3 vector in vectors)
            {
                yield return vector.ChooseRandomDirectionAround(maxHorizontalDegrees, maxVerticalDegreesDown, maxVerticalDegreesUp);
            }
        }

        public static Vector3 ChooseRandomDirectionAround(this Vector3 referenceVector, float maxHorizontalDegrees, float maxVerticalDegreesDown, float maxVerticalDegreesUp)
        {
            float yawChange = UnityEngine.Random.Range(-maxHorizontalDegrees, maxHorizontalDegrees);
            float pitchChange = UnityEngine.Random.Range(-maxVerticalDegreesDown, maxVerticalDegreesUp);

            Quaternion yawRotation = Quaternion.AngleAxis(yawChange, Vector3.up);

            Vector3 rightAxis = yawRotation * Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, rightAxis);

            Vector3 lookDirection = (yawRotation * pitchRotation * referenceVector).normalized;
            return lookDirection;
        }

        public static Vector3 ApplyRandomPitch(this Vector3 referenceVector, float maxVerticalDegreesDown, float maxVerticalDegreesUp)
        {
            Vector3 referenceVectorNormalized = referenceVector.normalized;
            Vector3 rightAxis = Vector3.Cross(Vector3.up, referenceVectorNormalized).normalized;

            float pitchChange = UnityEngine.Random.Range(-1 * maxVerticalDegreesDown, maxVerticalDegreesUp);
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, rightAxis);

            return (pitchRotation * referenceVectorNormalized).normalized;
        }
    }
}
