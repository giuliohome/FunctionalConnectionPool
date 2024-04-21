open System.Threading.Tasks
open Npgsql.FSharp
open Npgsql

let connectionString : string =
    Sql.host "localhost"
    |> Sql.database "test_db"
    |> Sql.username "test_user"
    |> Sql.password "test123"
    |> Sql.port 5432
    |> Sql.formatConnectionString

// construct the connection pool
let maxConn = 5
let pool = 
    [|1..maxConn|]
    |> Array.map (fun _i ->
        let singleton = new NpgsqlConnection(connectionString)
        singleton.Open()
        singleton
    )
    

let checkConnectionPool (singleton: NpgsqlConnection) : Task<int list> =
    singleton
    |> Sql.existingConnection
    |> Sql.query "select count(*) as conn_num from pg_stat_activity where usename='test_user';"
    |> Sql.executeAsync (fun read ->
        read.int "conn_num")


type Distributor = { Id: int; Name: string; }
let getDistributors (singleton: NpgsqlConnection) (myid: int): Async<Distributor list> = async {
    let! res =
        singleton
        |> Sql.existingConnection
        |> Sql.query "SELECT * FROM distributors WHERE did = @id"
        |> Sql.parameters [ "@id", Sql.int myid ]
        |> Sql.executeAsync (fun read ->
            {
                Id = read.int "did"
                Name = read.text "name"

            })
        |> Async.AwaitTask
    let! connNum = checkConnectionPool singleton |> Async.AwaitTask
    connNum
    |> List.head
    |> printfn "connections: %d" 
    return res
    }


async {
    printfn "start"
    let tasks = 
        [|1..30|]
        |> Array.chunkBySize maxConn
        |> Array.map (fun chunk -> 
            Array.map2 
                (fun  singleton id ->
                    getDistributors singleton id)
                pool
                chunk
            |> Async.Parallel
        )
        
    printfn "tasks ready"
    let tasks = 
        tasks
        |> Async.Sequential
    printfn "tasks sequentiated"
    
    let! tasks = tasks
    tasks
    |> Array.concat
    |> Array.iter(
        List.iter (fun d -> 
            printfn "name : %s id %d" d.Name d.Id)
    )
    
    printfn "end"
} 
|> Async.RunSynchronously
  