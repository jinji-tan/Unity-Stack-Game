using System.Collections; 
using System.Collections.Generic;
using UnityEngine;
using TMPro; 
using UnityEngine.SceneManagement; 

public class StackManager : MonoBehaviour
{
    public GameObject blockPrefab;
    public Camera mainCamera;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI gameOverText;
    
    public AudioClip dropSound;
    private AudioSource audioSource;
    private List<Transform> blocks = new List<Transform>();
    private Transform currentBlock;
    private Transform lastBlock;

    private bool gameOver = false;
    private int score = 0;
    
    private int combo = 0;
    private float maxScale = 5f; 

    private float blockHeight = 0.5f;
    private float speed = 5f;
    private bool movingX = true;
    private int direction = 1;
    private float bounds = 4.5f; 
    private float tolerance = 0.15f;

    void Start()
    {
        gameOverText.gameObject.SetActive(false);
        
        audioSource = gameObject.AddComponent<AudioSource>();
        
        GameObject baseBlock = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        baseBlock.transform.localScale = new Vector3(maxScale, blockHeight, maxScale); 
        SetBlockColor(baseBlock, 0);
        
        blocks.Add(baseBlock.transform);
        lastBlock = baseBlock.transform;

        SpawnNext();
    }

    void Update()
    {
        if (gameOver)
        {
            if (Input.GetMouseButtonDown(0))
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (movingX)
        {
            currentBlock.position += Vector3.right * speed * direction * Time.deltaTime;
            if (currentBlock.position.x > bounds || currentBlock.position.x < -bounds)
                direction *= -1;
        }
        else
        {
            currentBlock.position += Vector3.forward * speed * direction * Time.deltaTime;
            if (currentBlock.position.z > bounds || currentBlock.position.z < -bounds)
                direction *= -1;
        }

        if (Input.GetMouseButtonDown(0))
        {
            ChopBlock();
        }
    }

    void ChopBlock()
    {
        if (movingX)
        {
            float diff = currentBlock.position.x - lastBlock.position.x;
            
            if (Mathf.Abs(diff) <= tolerance)
            {
                currentBlock.position = new Vector3(lastBlock.position.x, currentBlock.position.y, currentBlock.position.z);
                
                combo++;
                if (combo > 5) GrowBlockSize();

                audioSource.pitch = Mathf.Clamp(1f + (combo * 0.1f), 1f, 2.5f);
                if (dropSound != null) audioSource.PlayOneShot(dropSound);

                StartCoroutine(PulseBlock(currentBlock)); 
                SuccessfulDrop();
            }
            else if (Mathf.Abs(diff) >= lastBlock.localScale.x)
            {
                GameOver();
            }
            else
            {
                combo = 0;
                audioSource.pitch = 1f;
                if (dropSound != null) audioSource.PlayOneShot(dropSound);

                float newW = lastBlock.localScale.x - Mathf.Abs(diff);
                float newX = lastBlock.position.x + diff / 2f;
                
                float fallingW = Mathf.Abs(diff);
                float fallingX = newX + (newW / 2f + fallingW / 2f) * Mathf.Sign(diff);

                currentBlock.localScale = new Vector3(newW, blockHeight, currentBlock.localScale.z);
                currentBlock.position = new Vector3(newX, currentBlock.position.y, currentBlock.position.z);
                
                SpawnFallingPiece(new Vector3(fallingX, currentBlock.position.y, currentBlock.position.z), 
                                  new Vector3(fallingW, blockHeight, currentBlock.localScale.z));

                SuccessfulDrop();
            }
        }
        else 
        {
            float diff = currentBlock.position.z - lastBlock.position.z;
            
            if (Mathf.Abs(diff) <= tolerance)
            {
                currentBlock.position = new Vector3(currentBlock.position.x, currentBlock.position.y, lastBlock.position.z);
                
                combo++;
                if (combo > 5) GrowBlockSize();

                audioSource.pitch = Mathf.Clamp(1f + (combo * 0.1f), 1f, 2.5f);
                if (dropSound != null) audioSource.PlayOneShot(dropSound);

                StartCoroutine(PulseBlock(currentBlock)); 
                SuccessfulDrop();
            }
            else if (Mathf.Abs(diff) >= lastBlock.localScale.z)
            {
                GameOver();
            }
            else
            {
                combo = 0;
                audioSource.pitch = 1f;
                if (dropSound != null) audioSource.PlayOneShot(dropSound);

                float newD = lastBlock.localScale.z - Mathf.Abs(diff);
                float newZ = lastBlock.position.z + diff / 2f;

                float fallingD = Mathf.Abs(diff);
                float fallingZ = newZ + (newD / 2f + fallingD / 2f) * Mathf.Sign(diff);

                currentBlock.localScale = new Vector3(currentBlock.localScale.x, blockHeight, newD);
                currentBlock.position = new Vector3(currentBlock.position.x, currentBlock.position.y, newZ);

                SpawnFallingPiece(new Vector3(currentBlock.position.x, currentBlock.position.y, fallingZ), 
                                  new Vector3(currentBlock.localScale.x, blockHeight, fallingD));

                SuccessfulDrop();
            }
        }
    }

    void GrowBlockSize()
    {
        float growAmount = 0.2f; 
        Vector3 newScale = currentBlock.localScale;
        
        newScale.x = Mathf.Clamp(newScale.x + growAmount, 0, maxScale);
        newScale.z = Mathf.Clamp(newScale.z + growAmount, 0, maxScale);
        
        currentBlock.localScale = newScale;
    }

    IEnumerator PulseBlock(Transform block)
    {
        Vector3 originalScale = block.localScale;
        Vector3 expandedScale = originalScale + new Vector3(0.2f, 0f, 0.2f); 
        float duration = 0.1f;
        float elapsed = 0;

        while (elapsed < duration)
        {
            if (block == null) yield break; 
            block.localScale = Vector3.Lerp(originalScale, expandedScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null; 
        }

        elapsed = 0;
        while (elapsed < duration)
        {
            if (block == null) yield break;
            block.localScale = Vector3.Lerp(expandedScale, originalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (block != null) block.localScale = originalScale;
    }

    void SpawnFallingPiece(Vector3 pos, Vector3 scale)
    {
        GameObject fallingBlock = Instantiate(blockPrefab, pos, Quaternion.identity);
        fallingBlock.transform.localScale = scale;
        SetBlockColor(fallingBlock, blocks.Count);
        fallingBlock.AddComponent<Rigidbody>();
        Destroy(fallingBlock, 3f);
    }

    void SuccessfulDrop()
    {
        score++;
        scoreText.text = score.ToString();
        speed += 0.2f;

        blocks.Add(currentBlock);
        lastBlock = currentBlock;

        mainCamera.transform.position += Vector3.up * blockHeight;
        SpawnNext();
    }

    void SpawnNext()
    {
        movingX = blocks.Count % 2 == 1;
        Vector3 spawnPos = lastBlock.position + Vector3.up * blockHeight;

        if (movingX) spawnPos.x = -bounds;
        else spawnPos.z = -bounds;

        direction = 1;

        GameObject newBlock = Instantiate(blockPrefab, spawnPos, Quaternion.identity);
        newBlock.transform.localScale = lastBlock.localScale; 
        SetBlockColor(newBlock, blocks.Count);
        currentBlock = newBlock.transform;
    }

    void SetBlockColor(GameObject block, int index)
    {
        float hue = ((index * 8f) % 360f) / 360f;
        Renderer renderer = block.GetComponent<Renderer>();
        renderer.material.color = Color.HSVToRGB(hue, 0.8f, 0.9f);
    }

    void GameOver()
    {
        gameOver = true;
        gameOverText.gameObject.SetActive(true);
        currentBlock.gameObject.AddComponent<Rigidbody>(); 
    }
}