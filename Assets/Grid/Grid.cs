using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mine
{
    public enum FindType
    {
        BFS,
        GBFS,
        DFS,
        Dijkstra,
        AStar,
        AStar2,
        IDAStar,
        JPS
    }

    public class Grid : MonoBehaviour
    {
        public static Grid ins;
        public GameObject tilePrefab;

        public Vector2Int gridSize = new Vector2Int(20, 20);
        public float NotWalkableTileGenerationProbability = 0.2f;

        public List<List<Tile>> tiles = new List<List<Tile>>();

        public Tile start, end;

        public Coroutine CurrentTask;

        public FindType findType = FindType.BFS;

        public float VisitedColorBlendLerp = 0.3f;

        public List<Coroutine> coroutines = new List<Coroutine>();

        public int AStarType = 2;
        public float AstarCrossMultiple = 0.2f;
        public float speedMultiple = 1f;

        public bool ShowProcess = true;

        public bool RandomBlocker = false;

        public bool RandomCost = true;

        public TextMeshProUGUI costCountTxt;

        void Awake() 
        {
            if(ins)
            {
                Destroy(gameObject);
                return;
            }

            ins = this;
        }

        // Start is called before the first frame update
        void Start()
        {
            //����Tile
            for (int i = 0; i < gridSize.x; i++)
            {
                tiles.Add(new List<Tile>());
                for (int j = 0; j < gridSize.y; j++)
                {
                    GameObject tile = Instantiate(tilePrefab, new Vector2(i, j) + new Vector2(transform.position.x, transform.position.y), Quaternion.identity);

                    Tile t = tile.GetComponent<Tile>();
                    t.randomCost = RandomCost;
                    t.setWalkable(Random.value > NotWalkableTileGenerationProbability || !RandomBlocker);

                    tiles[i].Add(t);
                    tile.transform.SetParent(transform);
                }
            }

            //���������ģ�Զ�������������
            Camera playerCamera = FindFirstObjectByType<Camera>();

            Vector3 pos = playerCamera.transform.position;
            pos.x = gridSize.x / 2 - 10;
            pos.y = gridSize.y / 2 - 10;
            playerCamera.transform.position = pos;

            int max = Mathf.Max(gridSize.x, gridSize.y);
            playerCamera.orthographicSize = max / 2;

            //��ʼ����������
            costCountTxt = GameObject.Find("CountTxt").GetComponent<TextMeshProUGUI>();
            GameObject.Find("RestartBtn").GetComponent<Button>().onClick.AddListener(Restart);
        }

        // Update is called once per frame
        void Update()
        {
            //���ÿ�ʼ��
            StartPointSelection();
            //���ý�����
            EndPointSelection();

            //����ǽ��
            SetBlocker();
        }

        void SetBlocker()
        {
            if (Input.GetMouseButton(2))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

                int x = (int)(pos.x + 0.5f);
                int y = (int)(pos.y + 0.5f);

                if (x >= 0 && y >= 0 && x < gridSize.x && y < gridSize.y)
                {
                    Tile tile = tiles[x][y];

                    if (tile != start && tile != end)
                    {
                        tile.setWalkable(false);
                    }
                }
            }
        }

        void StartPointSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

                int x = (int)(pos.x + 0.5f);
                int y = (int)(pos.y + 0.5f);

                if (x >= 0 && y >= 0 && x < gridSize.x && y < gridSize.y)
                {
                    Tile tile = tiles[x][y];

                    if(!tile.walkable)
                        return;
                    if (tile != end)
                    {
                        if (start)
                            start.setColor(Color.white);

                        if (tile == start)
                            start = null;
                        else
                        {
                            tile.setColor(Color.red, 1f);
                            start = tile;
                        }
                    }

                    StartFindWay();
                }
            } 
        }

        void EndPointSelection()
        {
            if(Input.GetMouseButtonDown(1))
            {
                Vector3 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;

                int x = (int)(pos.x + 0.5f);
                int y = (int)(pos.y + 0.5f);

                if (x >= 0 && y >= 0 && x < gridSize.x && y < gridSize.y)
                {
                    Tile tile = tiles[x][y];
                    if(!tile.walkable)
                        return;
                    if (tile != start)
                    {
                        if (end)
                            end.setColor(Color.white);

                        if (tile == end)
                            end = null;
                        else
                        {
                            tile.setColor(Color.blue, 1f);
                            end = tile;
                        }
                    }

                    StartFindWay();
                }
            }        
        }
        
        public void StartFindWay()
        {
            if(CurrentTask != null)
                StopCoroutine(CurrentTask);

            if(coroutines.Count > 0)
            {
                foreach(var c in coroutines)
                    if(c != null)
                        StopCoroutine(c);
            }
            coroutines.Clear();

            resetGrid();

            if (!start || !end)
                return;

            switch(findType)
            {
                case FindType.BFS:
                    CurrentTask = StartCoroutine(BfsPathFindingTask());
                    break;
                case FindType.GBFS:
                    CurrentTask = StartCoroutine(GBfsPathFindingTask());
                    break;
                case FindType.DFS:
                    CurrentTask = StartCoroutine(DfsPathFindingTask(start, () => {}));
                    break;
                case FindType.Dijkstra:
                    CurrentTask = StartCoroutine(DijkstraPathFindingTask());
                    break;
                case FindType.AStar:
                    CurrentTask = StartCoroutine(AStarPathFindingTask_1());
                    break;
                case FindType.AStar2:
                    CurrentTask = StartCoroutine(AStarPathFindingTask_2());
                    break;
            }
        }

        void resetGrid()
        {
            foreach (var list in tiles)
            {
                foreach (Tile tile in list)
                {
                    if (tile.walkable)
                    {
                        tile.visited = false;
                        if (tile != start && tile != end)
                        {
                            tile.setColor(Color.white);
                        }
                    }
                }
            }
        }
        Vector2Int startCoord => new Vector2Int((int)start.transform.localPosition.x, (int)start.transform.localPosition.y);
        Vector2Int endCoord => new Vector2Int((int)end.transform.localPosition.x, (int)end.transform.localPosition.y);
        Vector2Int[] dir = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        IEnumerator BfsPathFindingTask()
        {
            Queue<Tile> queue = new Queue<Tile>();                              //��������
            Dictionary<Tile, Tile> came_from = new Dictionary<Tile, Tile>();    //ǰ������
            queue.Enqueue(start);
            came_from[start] = null;

            while(queue.Count > 0)
            {
                //���ʵ�ǰTile
                Tile cur = queue.Dequeue();
                Vector2Int curCoord = getCoordByTile(cur);

                //���η������ĸ��ھӣ�����������δ�����ʹ������
                foreach(var d in dir)
                {
                    //��ȡ�ھ�����
                    Vector2Int tmp = curCoord + d;

                    //�������ȡ�ھ�
                    Tile neighbor = getTileByCoord(tmp);

                    //ȷ���ھӿɷ�����û��ǰ����û�з��ʹ���
                    if (neighbor && neighbor.walkable && !came_from.ContainsKey(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //��Ӹ��ھӣ�����ѷ��ʣ���¼�丸tile
                        queue.Enqueue(neighbor);
                        came_from[neighbor] = cur;

                        //��ֹ����
                        if(CheckFindPathComplete(neighbor, came_from))
                            yield break;
                    }
                }

                if(ShowProcess)
                    yield return new WaitForSeconds(0.001f / (0.001f * speedMultiple));
            }
        }
    
        IEnumerator GBfsPathFindingTask()
        {
            SortedDictionary<float, LinkedList<Tile>> queue = new SortedDictionary<float, LinkedList<Tile>>(){{0, new LinkedList<Tile>()}};
            Dictionary<Tile, Tile> came_from = new Dictionary<Tile, Tile>();
            queue[0].AddLast(start);
            came_from[start] = null;

            Vector2Int endCoord = getCoordByTile(end);

            while(queue.Count > 0)
            {
                //���ʵ�ǰTile
                Tile cur = queue.First().Value.First();
                //��ǰTile����
                if(queue.First().Value.Count > 1)
                    queue.First().Value.RemoveFirst();
                else
                    queue.Remove(queue.First().Key);

                //��ȡ��ǰTile����
                Vector2Int curCoord = getCoordByTile(cur);

                //���η������ĸ��ھӣ�����������δ�����ʹ������
                foreach(var d in dir)
                {
                    //�����ھ�����
                    Vector2Int tmp = curCoord + d;
                    //�������ȡ�ھ�
                    Tile neighbor = getTileByCoord(tmp);

                    //ȷ���ھӿɷ���
                    if (neighbor && neighbor.walkable && !came_from.ContainsKey(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //�������ȼ�
                        float priority = Vector2Int.Distance(tmp, endCoord);

                        //���
                        if(!queue.ContainsKey(priority))
                            queue.Add(priority, new LinkedList<Tile>());
                        queue[priority].AddLast(neighbor);

                        //����ǰ��
                        came_from[neighbor] = cur;

                        //��ֹ����
                        if(CheckFindPathComplete(neighbor, came_from))
                            yield break;
                    }
                }
                if(ShowProcess)
                    yield return new WaitForSeconds(0.01f / (0.01f * speedMultiple));
            }
        }

        IEnumerator DfsPathFindingTask(Tile tile, System.Action isDone)
        {
            if(end.visited || !tile)
            {
                isDone();
                yield break;
            }

            tile.visited = true;
            Vector2Int cur = getCoordByTile(tile);

            Tile neighbor = null;
            float minDis = float.MaxValue;
            foreach(var d in dir)
            {
                Vector2Int tmp = cur + d;
                Tile tmpTile = getTileByCoord(tmp);
                if(tmpTile && tmpTile.walkable && !tmpTile.visited)
                {
                    float dis = Vector2.Distance(tmp, endCoord);
                    if(dis < minDis)
                    {
                        neighbor = tmpTile;
                        minDis = dis;
                    }
                }
            }  

            if(neighbor)
            {
                if(neighbor != end)
                {
                    neighbor.setColor(Color.black, VisitedColorBlendLerp);
                }

                neighbor.visited = true;
                neighbor.pre = tile;

                if(neighbor == end)
                {
                    float costCount = neighbor.cost;
                    neighbor = neighbor.pre;
                    List<Tile> path = new List<Tile>();

                    while(neighbor != start)
                    {
                        costCount += neighbor.cost;
                        path.Add(neighbor);
                        neighbor = neighbor.pre;
                    }
                    costCount += start.cost;
                    Debug.Log(costCount);
                    path.Reverse();

                    foreach(Tile t in path)
                    {
                        t.setColor(Color.green, 0.5f);

                        yield return new WaitForSeconds(0.02f);
                    }

                    yield break;
                }

                if(ShowProcess)
                    yield return new WaitForSeconds(0.01f / (0.01f * speedMultiple));

                bool isdone = false;
                coroutines.Add(StartCoroutine(DfsPathFindingTask(neighbor, () => { isdone = true; })));
                yield return new WaitUntil(() => isdone);
            }
            else
            {
                bool isdone = false;
                coroutines.Add(StartCoroutine(DfsPathFindingTask(tile.pre, () => { isdone = true; })));
                yield return new WaitUntil(() => isdone);
            }

            isDone();
        }
        
        IEnumerator DijkstraPathFindingTask()
        {
            SortedDictionary<float, LinkedList<Tile>> queue = new SortedDictionary<float, LinkedList<Tile>>(){{0, new LinkedList<Tile>()}};
            Dictionary<Tile, Tile> came_from = new Dictionary<Tile, Tile>();
            Dictionary<Tile, float> cost_so_far = new Dictionary<Tile, float>();
            queue[0].AddLast(start);
            came_from[start] = null;
            cost_so_far[start] = 0;

            while(queue.Count > 0)
            {
                //���ʵ�ǰTile
                Tile cur = queue.First().Value.First();
                //��ǰTile����
                if(queue.First().Value.Count > 1)
                    queue.First().Value.RemoveFirst();
                else
                    queue.Remove(queue.First().Key);

                //��ȡ��ǰTile����
                Vector2Int curCoord = getCoordByTile(cur);

                //���η������ĸ��ھӣ�����������δ�����ʹ������
                foreach(var d in dir)
                {
                    //�����ھ�����
                    Vector2Int tmp = curCoord + d;
                    //�������ȡ�ھ�
                    Tile neighbor = getTileByCoord(tmp);
                    if(!neighbor)
                        continue;

                    //����cost
                    float new_cost = cost_so_far[cur] + neighbor.cost;
                    //�����ߣ��ҵ�һ���߻��ߴ�Ϊ����·��
                    if (neighbor.walkable && (!cost_so_far.ContainsKey(neighbor) || new_cost < cost_so_far[neighbor]))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //���
                        if(!queue.ContainsKey(new_cost))
                            queue.Add(new_cost, new LinkedList<Tile>());
                        queue[new_cost].AddLast(neighbor);
                        //����ǰ��
                        came_from[neighbor] = cur;
                        //����cost
                        cost_so_far[neighbor] = new_cost;

                        //��ֹ����
                        if(CheckFindPathComplete(neighbor, came_from))
                            yield break;
                    }
                }

                if(ShowProcess)
                    yield return new WaitForSeconds(0.001f / (0.001f * speedMultiple));
            }
        }

        IEnumerator AStarPathFindingTask_1()
        {
            SortedDictionary<float, LinkedList<Tile>> openQueue = new SortedDictionary<float, LinkedList<Tile>>(){{0, new LinkedList<Tile>()}};
            Dictionary<Tile, Tile> preDic = new Dictionary<Tile, Tile>();
            Dictionary<Tile, float> costDic = new Dictionary<Tile, float>();

            //��start��ʼ������
            openQueue[0].AddLast(start);
            preDic[start] = null;
            costDic[start] = 0;

            Vector2Int endCoord = getCoordByTile(end);

            bool wait;
            while(openQueue.Count > 0)
            {
                wait = false;

                //���ʵ�ǰTile
                Tile cur = openQueue.First().Value.First();
                //��ǰTile����
                if(openQueue.First().Value.Count > 1)
                    openQueue.First().Value.RemoveFirst();
                else
                    openQueue.Remove(openQueue.First().Key);

                //��ȡ��ǰTile����
                Vector2Int curCoord = getCoordByTile(cur);

                //���η������ĸ��ھӣ�����������δ�����ʹ������
                foreach(var d in dir)
                {
                    //�����ھ�����
                    Vector2Int tmp = curCoord + d;
                    //�������ȡ�ھ�
                    Tile neighbor = getTileByCoord(tmp);
                    if(!neighbor)
                        continue;

                    //�����ھӵ�Cost
                    float new_cost = costDic[cur] + neighbor.cost;
                    //�����ߣ��ҵ�һ���߻��ߴ�Ϊ����·��
                    if (neighbor.walkable && (!costDic.ContainsKey(neighbor) || new_cost < costDic[neighbor]))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //����cost��G)
                        costDic[neighbor] = new_cost;

                        //F = G+H��switch����Ҫ�ǲ�ͬ��H�ļ��㷽ʽ
                        switch(AStarType)
                        {
                            case 0:
                                new_cost += Vector2Int.Distance(tmp, endCoord);
                            break;
                            case 1:
                                float dx = Mathf.Abs(tmp.x - endCoord.x);
                                float dy = Mathf.Abs(tmp.y - endCoord.y);
                                new_cost += dx + dy + (Mathf.Sqrt(2) - 2) * Mathf.Min(dx, dy);
                            break;
                            case 2:
                                float dx1 = Mathf.Abs(tmp.x - endCoord.x);
                                float dy1 = Mathf.Abs(tmp.y - endCoord.y);
                                float dx2 = Mathf.Abs(startCoord.x - endCoord.x);
                                float dy2 = Mathf.Abs(startCoord.y - endCoord.y);
                                float cross = dx1 * dy2 - dx2 * dy1;
                                new_cost += neighbor.GetManhattanDistance(end) + (cross < 0 ? (cross + 1) * -2 : cross) * AstarCrossMultiple;
                            break;
                        }

                        //���
                        if(!openQueue.ContainsKey(new_cost))
                            openQueue.Add(new_cost, new LinkedList<Tile>());
                        openQueue[new_cost].AddLast(neighbor);

                        //��¼ǰ��
                        preDic[neighbor] = cur;

                        //��ֹ����
                        if(CheckFindPathComplete(neighbor, preDic))
                            yield break;

                        wait = true;
                    }
                }

                if(wait && ShowProcess)
                    yield return new WaitForSeconds(0.001f / (0.001f * speedMultiple));
            }
        }

        IEnumerator AStarPathFindingTask_2()
        {
            List<Tile> searchList = new List<Tile>() { start };
            List<Tile> processed = new List<Tile>();

            bool wait;
            while(searchList.Any())
            {
                wait = false;

                //ѡȡ���������ȼ���ߵĽڵ�
                Tile cur = searchList.First();
                foreach(Tile tile in searchList)
                {
                    if(tile.F < cur.F || tile.F == cur.F && tile.H < cur.H)
                        cur = tile;
                }

                //���Ӵ��������У���������������
                searchList.Remove(cur);
                processed.Add(cur);

                Vector2Int curCoord = getCoordByTile(cur);

                //���������ھӽڵ�
                foreach(Vector2Int d in dir)
                {
                    Vector2Int neighborCoord = curCoord + d;
                    Tile neighbor = getTileByCoord(neighborCoord);

                    //ȷ���������ϴ����ھӣ����ھӿ����ߣ��Ҳ�δ��ȷ����path��
                    if(neighbor && neighbor.walkable && !processed.Contains(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }

                        bool isSearched = searchList.Contains(neighbor);
                        //��ʹ�������پ��룬ʹ��neighbor��cost���Ǵ�Ȩ�ص�A*������Ȩ����ʵ����1
                        float cost = cur.G + cur.GetManhattanDistance(neighbor);    

                        if(!isSearched || cost < neighbor.G)
                        {
                            neighbor.G = cost;
                            neighbor.pre = cur;

                            if(!isSearched)
                            {
                                neighbor.H = neighbor.GetManhattanDistance(end);
                                searchList.Add(neighbor);
                            }

                            if(neighbor == end)
                            {
                                float costCount = 0f;
                                costCount += neighbor.cost;

                                neighbor = neighbor.pre;
                                while(neighbor != start)
                                {
                                    costCount += neighbor.cost;
                                    neighbor.setColor(Color.green, 0.5f);
                                    neighbor = neighbor.pre;
                                }
                                costCount += neighbor.cost;
                                Debug.Log(costCount);
                                costCountTxt.text = costCount.ToString();
                                yield break;
                            }
                        }

                        wait = true;
                    }
                }
                
                if(wait && ShowProcess)
                    yield return new WaitForSeconds(0.001f / (0.001f * speedMultiple));
            }

            yield break;
        }

        IEnumerator JPSPathFindingTask()
        {
            yield break;
        }

        public bool CheckFindPathComplete(Tile neighbor, Dictionary<Tile, Tile> came_from)
        {
            if(neighbor == end)
            {
                float costCount = 0f;
                costCount += neighbor.cost;

                neighbor = came_from[neighbor];
                while(neighbor != start)
                {
                    costCount += neighbor.cost;
                    neighbor.setColor(Color.green, 0.5f);
                    neighbor = came_from[neighbor];
                }
                costCount += neighbor.cost;
                Debug.Log(costCount);
                costCountTxt.text = costCount.ToString();
                return true;
            }
            return false;
        }

        public static Vector2Int getCoordByTile(Tile tile)
        {
            return new Vector2Int((int)tile.transform.localPosition.x, (int)tile.transform.localPosition.y);
        }
        public static Tile getTileByCoord(Vector2Int coord)
        {
            if (coord.x < 0 || coord.y < 0 || coord.x >= Grid.ins.gridSize.x || coord.y >= Grid.ins.gridSize.y)
                return null;
            return Grid.ins.tiles[coord.x][coord.y];
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
