//
//  ADRS.h
//
//  Created by Nigel Redmon on 12/18/12.
//  EarLevel Engineering: earlevel.com
//  Copyright 2012 Nigel Redmon
//
//  For a complete explanation of the ADSR envelope generator and code,
//  read the series of articles by the author, starting here:
//  http://www.earlevel.com/main/2013/06/01/envelope-generators/
//
//  License:
//
//  This source code is provided as is, without warranty.
//  You may copy and distribute verbatim copies of this document.
//  You may modify and use this source code to create binary code for your own purposes, free or commercial.
//
//  1.01  2016-01-02  njr   added calcCoef to SetTargetRatio functions that were in the ADSR widget but missing in this code
//  1.02  2017-01-04  njr   in calcCoef, checked for rate 0, to support non-IEEE compliant compilers
//
//  Converted to C# by Jakob Schmid 2018.

using Unity.Mathematics;
using UnityEngine;

public struct ADSR
{
    private enum EnvState
    {
        env_idle = 0,
        env_attack,
        env_decay,
        env_sustain,
        env_release
    };

    private EnvState state;
    private float output;
    private float attackRate;
    private float decayRate;
    private float releaseRate;
    private float attackCoef;
    private float decayCoef;
    private float releaseCoef;
    private float sustainLevel;
    private float targetRatioA;
    private float targetRatioDR;
    private float attackBase;
    private float decayBase;
    private float releaseBase;

    public void Init()
    {
        this = default;
        Reset();
        SetAttackRate(0.01f);
        SetDecayRate(0.1f);
        SetReleaseRate(0.1f);
        SetSustainLevel(0.5f);
        SetTargetRatioA(0.3f);
        SetTargetRatioDR(0.0001f);
    }

    public bool IsIdle => state == EnvState.env_idle;

    public void SetAttackRate(float rate)
    {
        attackRate = rate;
        if (rate <= 0)
        {
            attackCoef = 0;
            attackBase = 1.0f;
        }
        else
        {
            attackCoef = CalcCoef(rate, targetRatioA);
            attackBase = (1.0f + targetRatioA) * (1.0f - attackCoef);
        }
    }

    public void SetDecayRate(float rate)
    {
        decayRate = rate;
        if (rate <= 0)
        {
            decayCoef = 0;
            decayBase = sustainLevel;
        }
        else
        {
            decayCoef = CalcCoef(rate, targetRatioDR);
            decayBase = (sustainLevel - targetRatioDR) * (1.0f - decayCoef);
        }
    }

    public void SetReleaseRate(float rate)
    {
        releaseRate = rate;
        if (rate <= 0)
        {
            releaseCoef = 0;
            releaseBase = 0;
        }
        else
        {
            releaseCoef = CalcCoef(rate, targetRatioDR);
            releaseBase = -targetRatioDR * (1.0f - releaseCoef);
        }
    }

    public void SetSustainLevel(float level)
    {
        sustainLevel = level;
        decayBase = (sustainLevel - targetRatioDR) * (1.0f - decayCoef);
    }

    public void SetTargetRatioA(float targetRatio)
    {
        if (targetRatio < 0.000000001f)
            targetRatio = 0.000000001f; // -180 dB
        targetRatioA = targetRatio;
        if (attackRate > 0)
        {
            attackCoef = CalcCoef(attackRate, targetRatioA);
            attackBase = (1.0f + targetRatioA) * (1.0f - attackCoef);
        }
    }

    public void SetTargetRatioDR(float targetRatio)
    {
        if (targetRatio < 0.000000001f)
            targetRatio = 0.000000001f; // -180 dB
        targetRatioDR = targetRatio;
        if (decayRate > 0)
        {
            decayCoef = CalcCoef(decayRate, targetRatioDR);
            decayBase = (sustainLevel - targetRatioDR) * (1.0f - decayCoef);
        }

        if (releaseRate > 0)
        {
            releaseCoef = CalcCoef(releaseRate, targetRatioDR);
            releaseBase = -targetRatioDR * (1.0f - releaseCoef);
        }
    }

    public void Reset()
    {
        state = EnvState.env_idle;
        output = 0.0f;
    }

    private float CalcCoef(float rate, float targetRatio)
    {
        return math.exp(-math.log((1.0f + targetRatio) / targetRatio) / rate);
    }


    public float Process()
    {
        switch (state)
        {
            case EnvState.env_idle:
                break;
            case EnvState.env_attack:
                output = attackBase + output * attackCoef;
                if (output >= 1.0f)
                {
                    output = 1.0f;
                    state = EnvState.env_decay;
                }

                break;
            case EnvState.env_decay:
                output = decayBase + output * decayCoef;
                if (output <= sustainLevel)
                {
                    output = sustainLevel;
                    state = EnvState.env_sustain;
                }

                break;
            case EnvState.env_sustain:
                break;
            case EnvState.env_release:
                output = releaseBase + output * releaseCoef;
                if (output <= 0.0f)
                {
                    output = 0.0f;
                    state = EnvState.env_idle;
                }

                break;
        }

        return output;
    }

    public void SetGate(bool gate)
    {
        if (gate && state != EnvState.env_idle)
        {
            state = EnvState.env_attack;
        }
        else if (state != EnvState.env_idle && state != EnvState.env_release)
        {
            state = EnvState.env_release;
        }
    }
}