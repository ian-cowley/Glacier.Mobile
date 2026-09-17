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
    /// Optimized with direct scalar fast-paths for N=1 and N=2 (99%+ of mobile touch interactions),
    /// Vector128 fast-path for N=4 (dual-touch gestures), and Vector512/Vector256/Vector128 batch loops.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ApplyInertiaDamping(
        Span<float> velocities, 
        float frictionCoefficient)
    {
        int count = velocities.Length;
        if (count == 0) return;

        ref float vRef = ref velocities[0];

        // Fast-path: N=1 (single 1D touch velocity, e.g. vertical scroll or horizontal swipe)
        if (count == 1)
        {
            float v = vRef * frictionCoefficient;
            vRef = MathF.Abs(v) < VelocityEpsilon ? 0.0f : v;
            return;
        }

        // Fast-path: N=2 (single 2D touch velocity X/Y)
        if (count == 2)
        {
            float v0 = vRef * frictionCoefficient;
            float v1 = Unsafe.Add(ref vRef, 1) * frictionCoefficient;
            vRef = MathF.Abs(v0) < VelocityEpsilon ? 0.0f : v0;
            Unsafe.Add(ref vRef, 1) = MathF.Abs(v1) < VelocityEpsilon ? 0.0f : v1;
            return;
        }

        // Fast-path: N=4 (dual-touch gestures: pinch/rotate with X1, Y1, X2, Y2)
        if (count == 4 && Vector128.IsHardwareAccelerated)
        {
            var vFriction = Vector128.Create(frictionCoefficient);
            var vEps = Vector128.Create(VelocityEpsilon);
            var vZero = Vector128<float>.Zero;

            var v = Vector128.LoadUnsafe(ref vRef);
            var damped = v * vFriction;
            var vAbs = Vector128.Abs(damped);
            var vMask = Vector128.GreaterThanOrEqual(vAbs, vEps);
            damped = Vector128.ConditionalSelect(vMask, damped, vZero);
            damped.StoreUnsafe(ref vRef);
            return;
        }

        // General batch loop for large batches / simulators
        fixed (float* pVel = velocities)
        {
            int i = 0;

            if (Vector512.IsHardwareAccelerated && count >= 16)
            {
                var vFriction = Vector512.Create(frictionCoefficient);
                var vEps = Vector512.Create(VelocityEpsilon);
                var vZero = Vector512<float>.Zero;

                for (; i <= count - 16; i += 16)
                {
                    var v = Vector512.Load(pVel + i);
                    var damped = v * vFriction;
                    var vAbs = Vector512.Abs(damped);
                    var vMask = Vector512.GreaterThanOrEqual(vAbs, vEps);
                    damped = Vector512.ConditionalSelect(vMask, damped, vZero);
                    Vector512.Store(damped, pVel + i);
                }
            }

            if (Vector256.IsHardwareAccelerated && i <= count - 8)
            {
                var vFriction = Vector256.Create(frictionCoefficient);
                var vEps = Vector256.Create(VelocityEpsilon);
                var vZero = Vector256<float>.Zero;

                for (; i <= count - 8; i += 8)
                {
                    var v = Vector256.Load(pVel + i);
                    var damped = v * vFriction;
                    var vAbs = Vector256.Abs(damped);
                    var vMask = Vector256.GreaterThanOrEqual(vAbs, vEps);
                    damped = Vector256.ConditionalSelect(vMask, damped, vZero);
                    Vector256.Store(damped, pVel + i);
                }
            }

            if (Vector128.IsHardwareAccelerated && i <= count - 4)
            {
                var vFriction = Vector128.Create(frictionCoefficient);
                var vEps = Vector128.Create(VelocityEpsilon);
                var vZero = Vector128<float>.Zero;

                for (; i <= count - 4; i += 4)
                {
                    var v = Vector128.Load(pVel + i);
                    var damped = v * vFriction;
                    var vAbs = Vector128.Abs(damped);
                    var vMask = Vector128.GreaterThanOrEqual(vAbs, vEps);
                    damped = Vector128.ConditionalSelect(vMask, damped, vZero);
                    Vector128.Store(damped, pVel + i);
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
