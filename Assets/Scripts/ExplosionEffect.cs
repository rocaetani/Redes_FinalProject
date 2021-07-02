using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    private ParticleSystem _mainPS;
    public ParticleSystem PSLeft;
    public ParticleSystem PSRight;
    public ParticleSystem PSForward;
    public ParticleSystem PSBack;

    public float DistanceMultiplier;

    private int _indexToAdd;

    private void Awake()
    {
        _mainPS = GetComponent<ParticleSystem>();
    }

    public void SetPower(int power)
    {
        var mainForward = PSForward.main;
        mainForward.startSpeed = power * DistanceMultiplier;

        var mainLeft = PSLeft.main;
        mainLeft.startSpeed = power * DistanceMultiplier;

        var mainRight = PSRight.main;
        mainRight.startSpeed = power * DistanceMultiplier;

        var mainBack = PSBack.main;
        mainBack.startSpeed = power * DistanceMultiplier;
    }

    public void ActivateEffect()
    {
        _mainPS.Play();
    }
}
