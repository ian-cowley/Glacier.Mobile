namespace Glacier.Mobile.Input;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

/// <summary>
/// Hardware-accelerated gesture physics and touch velocity kernels.
/// Optimized for ARM64 AdvSimd (Neon) on mobile SOCs and AVX2 on desktop/simulators.
/// </summary>
public static unsafe class GestureKernels
{
    // Threshold to prevent asymptotic subnormal float penalties (< 1.175494e-38)
    public const float VelocityEpsilon = 0.001f;

    /// <summary>
    /// Applies exponential inertial decay across a batch of velocity vectors.
    /// Clamps asymptotic decay to zero when falling below VelocityEpsilon.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ApplyInertiaDamping(
        Span<float> velocities, 
        float frictionCoefficient)
    {
        int count = velocities.Length;
        if (count == 0) return;

        fixed (float* pVel = velocities)
        {
            int i = 0;

            // Tier 1: ARM64 AdvSimd (Neon) 4-lane single-precision vectorization
            if (AdvSimd.IsSupported && count >= 4)
            {
                var vFriction = Vector128.Create(frictionCoefficient);
                var vEps = Vector128.Create(VelocityEpsilon);
                var vZero = Vector128<float>.Zero;

                for (; i <= count - 4; i += 4)
                {
                    var v = AdvSimd.LoadVector128(pVel + i);
                    var damped = AdvSimd.Multiply(v, vFriction);
                    var vAbs = AdvSimd.Abs(damped);
                    var vMask = AdvSimd.CompareGreaterThanOrEqual(vAbs, vEps);
                    damped = AdvSimd.BitwiseSelect(vMask, damped, vZero);
                    AdvSimd.Store(pVel + i, damped);
                }
            }
            // Tier 2: x86/x64 AVX2 8-lane single-precision vectorization
            else if (Avx2.IsSupported && count >= 8)
            {
                var vFriction = Vector256.Create(frictionCoefficient);
                var vEps = Vector256.Create(VelocityEpsilon);
                var vZero = Vector256<float>.Zero;

                for (; i <= count - 8; i += 8)
                {
                    var v = Vector256.Load(pVel + i);
                    var damped = Vector256.Multiply(v, vFriction);
                    var vAbs = Vector256.Abs(damped);
                    var vMask = Vector256.GreaterThanOrEqual(vAbs, vEps);
                    damped = Vector256.ConditionalSelect(vMask, damped, vZero);
                    Vector256.Store(damped, pVel + i);
                }
            }

            // Remainder tail loop with subnormal clamp
            for (; i < count; i++)
            {
                float v = pVel[i] * frictionCoefficient;
                pVel[i] = MathF.Abs(v) < VelocityEpsilon ? 0.0f : v;
            }
        }
    }

    /// <summary>
    /// Computes instantaneous velocity in points per second from delta position and delta time.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ComputeVelocity(float deltaPos, float dt)
    {
        if (dt <= 0.00001f) return 0f;
        return deltaPos / dt;
    }
}
