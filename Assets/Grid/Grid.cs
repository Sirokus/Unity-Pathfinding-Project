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
            //生成Tile
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

            //根据网格规模自动放置玩家摄像机
            Camera playerCamera = FindFirstObjectByType<Camera>();

            Vector3 pos = playerCamera.transform.position;
            pos.x = gridSize.x / 2 - 10;
            pos.y = gridSize.y / 2 - 10;
            playerCamera.transform.position = pos;

            int max = Mathf.Max(gridSize.x, gridSize.y);
            playerCamera.orthographicSize = max / 2;

            //初始化部分引用
            costCountTxt = GameObject.Find("CountTxt").GetComponent<TextMeshProUGUI>();
            GameObject.Find("RestartBtn").GetComponent<Button>().onClick.AddListener(Restart);
        }

        // Update is called once per frame
        void Update()
        {
            //设置开始点
            StartPointSelection();
            //设置结束点
            EndPointSelection();

            //绘制墙体
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
            Queue<Tile> queue = new Queue<Tile>();                              //遍历队列
            Dictionary<Tile, Tile> came_from = new Dictionary<Tile, Tile>();    //前驱队列
            queue.Enqueue(start);
            came_from[start] = null;

            while(queue.Count > 0)
            {
                //访问当前Tile
                Tile cur = queue.Dequeue();
                Vector2Int curCoord = getCoordByTile(cur);

                //依次访问其四个邻居，若可行走且未被访问过则入队
                foreach(var d in dir)
                {
                    //获取邻居坐标
                    Vector2Int tmp = curCoord + d;

                    //按坐标获取邻居
                    Tile neighbor = getTileByCoord(tmp);

                    //确保邻居可访问且没有前驱（没有访问过）
                    if (neighbor && neighbor.walkable && !came_from.ContainsKey(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //入队该邻居，标记已访问，记录其父tile
                        queue.Enqueue(neighbor);
                        came_from[neighbor] = cur;

                        //终止条件
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
                //访问当前Tile
                Tile cur = queue.First().Value.First();
                //当前Tile出队
                if(queue.First().Value.Count > 1)
                    queue.First().Value.RemoveFirst();
                else
                    queue.Remove(queue.First().Key);

                //获取当前Tile坐标
                Vector2Int curCoord = getCoordByTile(cur);

                //依次访问其四个邻居，若可行走且未被访问过则入队
                foreach(var d in dir)
                {
                    //计算邻居坐标
                    Vector2Int tmp = curCoord + d;
                    //按坐标获取邻居
                    Tile neighbor = getTileByCoord(tmp);

                    //确保邻居可访问
                    if (neighbor && neighbor.walkable && !came_from.ContainsKey(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //计算优先级
                        float priority = Vector2Int.Distance(tmp, endCoord);

                        //入队
                        if(!queue.ContainsKey(priority))
                            queue.Add(priority, new LinkedList<Tile>());
                        queue[priority].AddLast(neighbor);

                        //设置前驱
                        came_from[neighbor] = cur;

                        //终止条件
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
                //访问当前Tile
                Tile cur = queue.First().Value.First();
                //当前Tile出队
                if(queue.First().Value.Count > 1)
                    queue.First().Value.RemoveFirst();
                else
                    queue.Remove(queue.First().Key);

                //获取当前Tile坐标
                Vector2Int curCoord = getCoordByTile(cur);

                //依次访问其四个邻居，若可行走且未被访问过则入队
                foreach(var d in dir)
                {
                    //计算邻居坐标
                    Vector2Int tmp = curCoord + d;
                    //按坐标获取邻居
                    Tile neighbor = getTileByCoord(tmp);
                    if(!neighbor)
                        continue;

                    //计算cost
                    float new_cost = cost_so_far[cur] + neighbor.cost;
                    //可行走，且第一次走或者此为更优路线
                    if (neighbor.walkable && (!cost_so_far.ContainsKey(neighbor) || new_cost < cost_so_far[neighbor]))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //入队
                        if(!queue.ContainsKey(new_cost))
                            queue.Add(new_cost, new LinkedList<Tile>());
                        queue[new_cost].AddLast(neighbor);
                        //设置前驱
                        came_from[neighbor] = cur;
                        //更新cost
                        cost_so_far[neighbor] = new_cost;

                        //终止条件
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

            //用start初始化容器
            openQueue[0].AddLast(start);
            preDic[start] = null;
            costDic[start] = 0;

            Vector2Int endCoord = getCoordByTile(end);

            bool wait;
            while(openQueue.Count > 0)
            {
                wait = false;

                //访问当前Tile
                Tile cur = openQueue.First().Value.First();
                //当前Tile出队
                if(openQueue.First().Value.Count > 1)
                    openQueue.First().Value.RemoveFirst();
                else
                    openQueue.Remove(openQueue.First().Key);

                //获取当前Tile坐标
                Vector2Int curCoord = getCoordByTile(cur);

                //依次访问其四个邻居，若可行走且未被访问过则入队
                foreach(var d in dir)
                {
                    //计算邻居坐标
                    Vector2Int tmp = curCoord + d;
                    //按坐标获取邻居
                    Tile neighbor = getTileByCoord(tmp);
                    if(!neighbor)
                        continue;

                    //计算邻居的Cost
                    float new_cost = costDic[cur] + neighbor.cost;
                    //可行走，且第一次走或者此为更优路线
                    if (neighbor.walkable && (!costDic.ContainsKey(neighbor) || new_cost < costDic[neighbor]))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }
                        
                        //更新cost（G)
                        costDic[neighbor] = new_cost;

                        //F = G+H，switch中主要是不同的H的计算方式
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

                        //入队
                        if(!openQueue.ContainsKey(new_cost))
                            openQueue.Add(new_cost, new LinkedList<Tile>());
                        openQueue[new_cost].AddLast(neighbor);

                        //记录前驱
                        preDic[neighbor] = cur;

                        //终止条件
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

                //选取队列中优先级最高的节点
                Tile cur = searchList.First();
                foreach(Tile tile in searchList)
                {
                    if(tile.F < cur.F || tile.F == cur.F && tile.H < cur.H)
                        cur = tile;
                }

                //出队待搜索队列，进队已搜索队列
                searchList.Remove(cur);
                processed.Add(cur);

                Vector2Int curCoord = getCoordByTile(cur);

                //遍历所有邻居节点
                foreach(Vector2Int d in dir)
                {
                    Vector2Int neighborCoord = curCoord + d;
                    Tile neighbor = getTileByCoord(neighborCoord);

                    //确保该坐标上存在邻居，且邻居可行走，且并未被确定在path中
                    if(neighbor && neighbor.walkable && !processed.Contains(neighbor))
                    {
                        if(neighbor != end)
                        {
                            neighbor.setColor(Color.black, VisitedColorBlendLerp);
                        }

                        bool isSearched = searchList.Contains(neighbor);
                        //不使用曼哈顿距离，使用neighbor的cost就是带权重的A*，不带权重其实就是1
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
