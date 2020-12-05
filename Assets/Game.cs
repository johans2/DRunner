using System;
using System.Collections;
using System.Collections.Generic;
using DigitalRubyShared;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class Game : MonoBehaviour {

    [Serializable]
    public struct TileAndProbability {
        public GameObject tilePrefab;
        public int probaability;
    }

    public GameObject runner;
    public float jumpForce = 10;
    public float startXTilePos = -2.32f;
    public float groundHeight = -1.05f;
    public float speed = 1f;

    public TileAndProbability[] tilesTypes;
    
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private List<GameObject> activeTile = new List<GameObject>();
    private GameObject tileAtFront;
    private int numGroundTiles = 8;
    private int delayframesatstart = 5;

    public void OnRestartGame () {
        SceneManager.LoadScene(0);
    }

    public void OnFireA () {
        Debug.Log("FIRE A");
    }

    public void OnFireB () {
        Debug.Log("FIRE B");
    }

    private void Awake() {
        var tileList = new List<GameObject>();
        for (int i = 0; i < tilesTypes.Length; i++) {
            var tandp = tilesTypes[i];
            for (int j = 0; j < tandp.probaability; j++) {
                var tile = Instantiate(tandp.tilePrefab);
                tileList.Add(tile);
                tile.SetActive(false);
            }
        }
        
        tileList.Shuffle();
        for (int i = 0; i < tileList.Count; i++) {
            tilePool.Enqueue(tileList[i]);
        }

        Physics.gravity = Vector3.one * -9.82f * 2f;
    }
    
    void Start() {
        for (int i = 0; i < numGroundTiles; i++) {
            var tile = tilePool.Dequeue();
            tile.SetActive(true);
            tile.transform.position = new Vector3(startXTilePos + i*10, groundHeight, 0);
            activeTile.Add(tile);
        }
        tileAtFront = activeTile[numGroundTiles - 1];
        CreateSwipeGesture();
    }

    private void CreateSwipeGesture()
    {
        var swipeGesture = new SwipeGestureRecognizer();
        swipeGesture.MinimumDistanceUnits = 0.01f;
        swipeGesture.Direction = SwipeGestureRecognizerDirection.Any;
        swipeGesture.StateUpdated += SwipeGestureCallback;
        swipeGesture.DirectionThreshold = 1.0f; // allow a swipe, regardless of slope
        FingersScript.Instance.AddGesture(swipeGesture);
    }
    
    private void SwipeGestureCallback(GestureRecognizer gesture)
    {
        if (gesture.State == GestureRecognizerState.Ended) {
            float jumpOrRoll = Math.Abs(gesture.DeltaY);
            float stabOrBlock = Math.Abs(gesture.DeltaX);

            if (jumpOrRoll > stabOrBlock) {
                
                if (gesture.DeltaY > 0.1) {
                    Debug.Log("JUMP!!");
                    if (runner.transform.position.y < -0.5f) {
                        runner.GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);                    
                    }
                }
                if (gesture.DeltaY < -0.1) {
                    Debug.Log("ROLL!!");
                    runner.GetComponentInChildren<Animator>().SetTrigger("Roll");
                }    
            }else {
                if (gesture.DeltaX > 0.1) {
                    Debug.Log("STAB!!");
                    if (!stabbing) {
                        runner.GetComponentInChildren<Animator>().SetTrigger("Dash");
                        StartCoroutine(DoStabMove());
                    }

                }                
            }



        }
    }

    private bool stabbing = false;
    public IEnumerator DoStabMove() {
        stabbing = true;
        float stabRange = 0.75f;
        float stabTime = 0.3f;
        float elapsedTime = 0;
        Vector3 startpos = runner.transform.position;
        while (elapsedTime <= stabTime) {
            elapsedTime += Time.deltaTime;
            runner.transform.position = Vector3.Lerp(startpos, startpos + new Vector3(stabRange, 0, 0), elapsedTime / stabTime);
            yield return null;
        }

        elapsedTime = 0;
        while (elapsedTime <= stabTime) {
            elapsedTime += Time.deltaTime;
            runner.transform.position = Vector3.Lerp(startpos + new Vector3(stabRange, 0, 0),startpos, elapsedTime / stabTime);
            yield return null;
        }

        stabbing = false;
    }

    void Update() {
        delayframesatstart--;
        if (delayframesatstart > 0) {
            return;
        }

        for (int i = 0; i < activeTile.Count; i++) {
            var tile = activeTile[i];
            tile.transform.position -= new Vector3(speed * Time.deltaTime,0,0);
            
            if (tile.transform.position.x < -20f) {
                tile.SetActive(false);
                tilePool.Enqueue(tile);
                activeTile.Remove(tile);

                var newTile = tilePool.Dequeue();
                newTile.SetActive(true);
                newTile.transform.position = tileAtFront.transform.position + new Vector3(10, 0, 0);
                activeTile.Add(newTile);
                tileAtFront = newTile;
            }
        }
    }
}
