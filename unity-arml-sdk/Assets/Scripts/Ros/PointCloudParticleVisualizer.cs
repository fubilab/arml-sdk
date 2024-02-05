using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(FeaturesSubscriber))]
public class PointCloudParticleVisualizer : MonoBehaviour
{
    void OnPointCloudChanged()
    {
        //Debug.Log("onPointCloudChanged " + _featuresSubscriber.positions.Count);
        var points = s_Vertices;
        points.Clear();

        if (_featuresSubscriber.positions != null)
        {
            foreach (var point in _featuresSubscriber.positions)
                s_Vertices.Add(point);
        }

        int numParticles = points.Count;
        if (_particles == null || _particles.Length < numParticles)
            _particles = new ParticleSystem.Particle[numParticles];

        var color = _particleSystem.main.startColor.color;
        var size = _particleSystem.main.startSize.constant;

        for (int i = 0; i < numParticles; ++i)
        {
            _particles[i].startColor = color;
            _particles[i].startSize = size;
            _particles[i].position = points[i];
            _particles[i].remainingLifetime = 1f;
        }

        // Remove any existing particles by setting remainingLifetime
        // to a negative value.
        for (int i = numParticles; i < _numParticles; ++i)
        {
            _particles[i].remainingLifetime = -1f;
        }

        _particleSystem.SetParticles(_particles, Math.Max(numParticles, _numParticles));
        _numParticles = numParticles;
    }

    void Awake()
    {
        _featuresSubscriber = GetComponent<FeaturesSubscriber>();
        _particleSystem = GetComponent<ParticleSystem>();
    }

    void Start()
    {
        SetVisible(true);
    }

    void OnEnable()
    {
        _featuresSubscriber.updated += OnPointCloudChanged;
        // UpdateVisibility();
    }

    void OnDisable()
    {
        _featuresSubscriber.updated -= OnPointCloudChanged;
        // UpdateVisibility();
    }

    void Update()
    {
        // UpdateVisibility();
    }

    // void UpdateVisibility()
    // {
    //     var visible =
    //         enabled &&
    //         (m_PointCloud.trackingState != TrackingState.None);

    //     SetVisible(visible);
    // }

    void SetVisible(bool visible)
    {
        if (_particleSystem == null)
            return;

        var renderer = _particleSystem.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = visible;
    }

    FeaturesSubscriber _featuresSubscriber;

    ParticleSystem _particleSystem;

    ParticleSystem.Particle[] _particles;

    int _numParticles;

    static List<Vector3> s_Vertices = new List<Vector3>();
}
