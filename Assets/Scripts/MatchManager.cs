using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAPI;

public class MatchManager : NetworkBehaviour
{
    public GameObject UnbreakableWallPrefab;

    public GameObject BreakableWallPrefab;

    public GameObject FloorPrefab;

    private const string _containerName = "MatchPrefabs";
    private Transform _prefabContainer;

    private void Awake()
    {
        _prefabContainer = gameObject.GetComponent<Transform>();
    }

    private void Start()
    {
        BuildMap(18, 12, 10);
    }

    public void BuildMap(int width, int height, int fillPercentage)
    {
        //validate if width or height are even then transform then in odd
        if (isEven(width))
        {
            width = width + 1;
        }
        if (isEven(height))
        {
            height = height + 1;
        }




        List<Vector3> listFreeSpaces = new List<Vector3>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                //instantiate floor
                Vector3 position = new Vector3(x*10+5, 0, z*10+5);

                Instantiate(FloorPrefab, position, Quaternion.identity, _prefabContainer);

                position.y = 5.5f;

                //instantiate unbreakable walls
                if (isEven(x) && isEven(z))
                {
                    Instantiate(UnbreakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    continue;
                }

                //instantiate edges
                if (x == 0 || x == width - 1 || z == 0 || z == height - 1)
                {
                    Instantiate(UnbreakableWallPrefab, position, Quaternion.identity, _prefabContainer);
                    continue;
                }

                //verify if is not player spawn places
                if(x == 1)
                {
                    if (z == 1 || z == 2 || z == height - 2 || z == height - 3)
                    {
                        continue;
                    }
                }
                if(x == 2)
                {
                    if (z == 1 || z == height - 2 )
                    {
                        continue;
                    }
                }
                if(x == width - 2)
                {
                    if (z == 1 || z == 2 || z == height - 2 || z == height - 3)
                    {
                        continue;
                    }
                }
                if(x == width - 3)
                {
                    if (z == 1 || z == height - 2 )
                    {
                        continue;
                    }
                }

                listFreeSpaces.Add(position);

            }
        }


        int numberOfBreakableWalls = listFreeSpaces.Count * fillPercentage / 100;
        Debug.Log(listFreeSpaces.Count);
        Debug.Log(numberOfBreakableWalls);
        while (numberOfBreakableWalls > 0)
        {
            int random = Random.Range(0, listFreeSpaces.Count - 1);
            Instantiate(BreakableWallPrefab, listFreeSpaces[random], Quaternion.identity, _prefabContainer);
            listFreeSpaces.RemoveAt(random);
            numberOfBreakableWalls--;
        }


        Vector3 playerPosition1 = new Vector3(1*10, 6, 1*10);
        Vector3 playerPosition2 = new Vector3(1*10, 6,  (height - 2)*10);
        Vector3 playerPosition3 = new Vector3((width - 2)*10, 6, 1*10);
        Vector3 playerPosition4 = new Vector3((width - 2)*10, 6, (height - 2)*10);
        // MatchManager.Instance.SpawnPlayers(playerPosition1, playerPosition2, playerPosition3, playerPosition4);
    }



    public bool isEven(int value)
    {
        return value % 2 == 0;
    }



}
