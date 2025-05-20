using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneScriptHeadInteraction : MonoBehaviour
{
    private const int MAX_BRUISES_COUNT = 100;
    [SerializeField] private Camera mainCamera;


    [SerializeField] private float chargeTime;
    [SerializeField] private float maxStrength;
    [SerializeField] private float maxRadius;
    [SerializeField] private AnimationCurve strengthCurve;
    [SerializeField] private AnimationCurve radiusCurve;

    [SerializeField] private Rigidbody headJoint;
    [SerializeField] private float maxForceApplied;
    [SerializeField] private AnimationCurve forceCurve;
    [SerializeField] private LayerMask headLayerMask;
    [SerializeField] private SkinnedMeshRenderer head;
    [SerializeField] private Transform headBone;
    [SerializeField] private float animationDuration;
    [SerializeField] private AnimationCurve strengthAnimateCurve;
    [SerializeField] private AnimationCurve radiusAnimationCurve;

    [SerializeField] private float torqueMultiplier = 100;

    [SerializeField] private ParticleSystem hitVFX;
    [SerializeField] private float destroyDelay = 2;

    private int bruisesCount = 0;
    private List<float> bruiseRadii;
    private List<float> bruisesStrength;
    private List<Vector4> bruisesPositions;
    private bool needUpdate;
    private float chargeDuration;

    private Material headMat;

    void Start()
    {
        bruiseRadii = new List<float>();
        bruisesPositions = new List<Vector4>();
        bruisesStrength = new List<float>();

        headMat = head.materials[1];

        headMat.SetMatrix("_WorldToRoot", headBone.worldToLocalMatrix);

        headMat.SetVectorArray("_BruisePositions", new List<Vector4>(new Vector4[MAX_BRUISES_COUNT]));
        headMat.SetFloatArray("_BruiseRadii", new List<float>(new float[MAX_BRUISES_COUNT]));
        headMat.SetFloatArray("_BruiseStrengths", new List<float>(new float[MAX_BRUISES_COUNT]));
    }

    public void AddBruise(Vector3 position, float targetStrength, float targetRadius)
    {
        int currentBruiseIndex = bruisesCount;
        if (bruisesCount == MAX_BRUISES_COUNT)
        {
            bruisesPositions.RemoveAt(0);
            bruisesStrength.RemoveAt(0);
            bruiseRadii.RemoveAt(0);
        }
        else
            bruisesCount++;

        //bruisesPositions.Add(head.transform.InverseTransformPoint(position));
        bruisesPositions.Add(headBone.InverseTransformPoint(position));
        bruisesStrength.Add(0);
        bruiseRadii.Add(0);
        headMat.SetInt("_BruiseCount", bruisesCount);
        StartCoroutine(AnimateBruisesAppear(targetStrength, targetRadius, currentBruiseIndex));
        var vfxInstance = Instantiate(hitVFX);
        vfxInstance.transform.position = position;

        Destroy(vfxInstance.gameObject, destroyDelay);
    }

    private IEnumerator AnimateBruisesAppear(float targetStrength, float targetRadius, int index)
    {
        float t = 0f;
        while (t < animationDuration)
        {
            t += Time.deltaTime;
            bruiseRadii[index] = Mathf.Lerp(0, targetRadius, radiusAnimationCurve.Evaluate(Mathf.InverseLerp(0, animationDuration, t)));
            bruisesStrength[index] = Mathf.Lerp(0, targetStrength, strengthAnimateCurve.Evaluate(Mathf.InverseLerp(0, animationDuration, t)));
            needUpdate = true;
            yield return null;
        }
    }

    private void UpdateBruisesSettings()
    {
        headMat.SetVectorArray("_BruisePositions", bruisesPositions);
        headMat.SetFloatArray("_BruiseRadii", bruiseRadii);
        headMat.SetFloatArray("_BruiseStrengths", bruisesStrength);
    }

    private void Update()
    {
        if (needUpdate)
        {
            UpdateBruisesSettings();

            needUpdate = false;
        }

        headMat.SetMatrix("_WorldToRoot", headBone.worldToLocalMatrix);
        if (Input.GetMouseButton(0))
        {
            chargeDuration += Time.deltaTime;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (Physics.Raycast(mainCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 200, headLayerMask))
            {

                float chargeProgression = Mathf.InverseLerp(0, chargeTime, chargeDuration);
                AddBruise(hit.point, strengthAnimateCurve.Evaluate(chargeProgression) * maxStrength, radiusAnimationCurve.Evaluate(chargeProgression) * maxRadius);

                var force = (hit.point - mainCamera.transform.position).normalized * forceCurve.Evaluate(chargeProgression) * maxForceApplied;

                headJoint.AddForceAtPosition(force, hit.point,ForceMode.VelocityChange);
                headJoint.AddTorque(CalculateTorque(headJoint, force, hit.point) * torqueMultiplier);
            }
            chargeDuration = 0;
        }
    }

    public Vector3 CalculateTorque(Rigidbody rigidbody, Vector3 force, Vector3 applicationPoint)
    {
        Vector3 centerOfMass = rigidbody.worldCenterOfMass;
        Vector3 offset = applicationPoint - centerOfMass;
        return Vector3.Cross(offset, force);
    }
}