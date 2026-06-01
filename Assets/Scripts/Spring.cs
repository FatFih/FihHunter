using UnityEngine;

public class Spring
{
    private float strength;
    private float damper;
    private float target;
    private float velocity;
    private float value;

    public void Update(float deltaTime)
    {
        float direction = target - value >= 0 ? 1f : -1f;
        float force = Mathf.Abs(target - value) * strength;
        velocity += (force * direction - velocity * damper) * deltaTime;
        value += velocity * deltaTime;
    }

    public void Reset()
    {
        velocity = 0f;
        value = 0f;
    }

    public void SetValue(float val) { value = val; }
    public void SetTarget(float t) { target = t; }
    public void SetDamper(float d) { damper = d; }
    public void SetStrength(float s) { strength = s; }
    public void SetVelocity(float v) { velocity = v; }

    public float Value => value;
    public float Velocity => velocity;
}
