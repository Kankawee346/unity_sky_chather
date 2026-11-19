using UnityEngine;

public sealed class CatchLabSpark : MonoBehaviour
{
    private CatchGame game;
    private Vector2 velocity;
    private float life=.5f;
    public void Configure(CatchGame owner,Vector2 direction) { game=owner; velocity=direction; }
    private void Update()
    {
        if(game==null || !game.Playing) { Destroy(gameObject); return; }
        if(game.Paused) return;
        float dt=Time.unscaledDeltaTime; life-=dt;
        if(life<=0) { Destroy(gameObject); return; }
        ((RectTransform)transform).anchoredPosition+=velocity*dt;
        velocity.y-=220*dt;
        transform.localScale=Vector3.one*Mathf.Clamp01(life*3);
    }
}
