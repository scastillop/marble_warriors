//creo el servidor
var express=require("express");
var app=express();

//genero variables para la visualizacion de datos en navegador
var debugPort = 8080;
app.get('/', function (req, res) {
	//hago un resumen d elos datos que quiero mostrar
	total = {};
	total.players = players;
	total.games = games;
	total.modularServers = modularServers;
	var cache = [];
	//envio los datos en Json
	res.send(JSON.stringify(total, function(key, value) {
		if (typeof value === 'object' && value !== null) {
			if (cache.indexOf(value) !== -1) {
				return;
			}
			cache.push(value);
		}
		return value;
	}));
});
app.listen(debugPort, function () {});

var server=require("http").createServer(app);
var io=require("socket.io").listen(server);
var port=9000;

//intancio la clase para conectarme a la db
var mysqlConnection = require('../databaseConnection/mysqlConnector');

//inicio el servidor
server.listen(port, function() {
	console.log(new Date(Date.now()).toLocaleString()+' Server started on port '+port+'...');
});

//aqui se guardan los servidores modulares
var modularServers = {};

//aqui se guardaran las partidas en curso
var games = [];

//aqui se guardan los datos escenciales de los jugadores (para acceder rapidamente a sus partidas)
var players = {};

io.on("connection",function(socket){
	//cuando un servidor modular se conecta
	socket.on("SuscribeServer", function(address, callback){
		//verifico que el servidor no estuviera previamente guardado
		var found="";
		for (var serverKey in modularServers){
			//si lo estaba
			if(modularServers[serverKey].address==address){
				//lo guardo
				found=serverKey;
			}
		}
		//si lo encontre
		if(found!=""){
			//lo elimino
			delete modularServers[found];
		}
		console.log(new Date(Date.now()).toLocaleString()+" Modular server connected from "+socket.request.connection.remoteAddress);
		//guardo el servidor modular en mi lista de servidores
		modularServers[socket.id] = {};
		modularServers[socket.id].socket = socket;
		modularServers[socket.id].address = address;
		modularServers[socket.id].games = 0;
		callback();
	});

	//cuando un jugador se conecta
	socket.on("SuscribeClient", function(data){
		//veo si el jugadore estaba anteriormente en partida
		if(players[data.email]&&games[players[data.email].gameIndex]&&games[players[data.email].gameIndex].players[players[data.email].playerIndex]&&games[players[data.email].gameIndex].players[players[data.email].playerIndex].status=="onGame"&&modularServers[games[players[data.email].gameIndex].serverKey]){
			//informo que se reconecto un cliente
			console.log(new Date(Date.now()).toLocaleString()+" Client ("+data.email+") re connected from "+socket.request.connection.remoteAddress);
			//si ya estaba guardo su nuevo socket
			players[data.email].socket = socket;
			games[players[data.email].gameIndex].players[players[data.email].playerIndex].socket=socket;
			//y lo envío a su juego
			socket.emit("SetServer", modularServers[games[players[data.email].gameIndex].serverKey].address);
		}else{
			//informo que se conecto un cliente
			console.log(new Date(Date.now()).toLocaleString()+" Client ("+data.email+") connected from "+socket.request.connection.remoteAddress);
			//de lo contrario guardo al jugador
			players[data.email] = {};
			players[data.email].socket = socket;
			players[data.email].email = data.email;
			players[data.email].status = "connected";
			players[data.email].name = data.name;

			//obtengo la informacion del jugador en db
			mysqlConnection.FindUserByEmail(data.email, function (result) {
				//verifico si existe el jugador
				if(result&&result.length>0){
					//si existe guardo su id y nombre
					players[data.email].id = result[0].id;
				}else{
					//de lo contrario creo el jugador en la base de datos
					mysqlConnection.InsertUser(data.email, data.name, function (result) {
						players[data.email].id=result.insertedId;
					});
				}
			});
		}
	});

	//cuando un jugador busca oponente
	socket.on("SearchOpponent", function(email){
		//busco si existe algun juego que esté esperando un jugador
		var opponentFound = false;
		for(var key in games){
			//busco juegos que tengan un solo jugador
			if(games[key].players.length==1){
				//si lo tiene agrego al jugador
				var player2 = {
					id: players[email].id,
					status: 'connected',
					socket: socket,
					email: email,
					name: players[email].name,
					index: 1
				};
				games[key].players.push(player2);
				//asocio los datos de la partida y del juegador a su socket, para luego acceder rapidamente a ellos
				players[email].gameIndex = key;
				players[email].playerIndex = 1;
				//procedo a generar un identificador unico para el juego
				var id="";
				//informo a los jugadores que ya se encontro su oponente
				for(var playerKey in games[key].players){
					games[key].players[playerKey].status="selectingChars";
					games[key].players[playerKey].socket.emit("OpponentFound",key);
					//agrego informacion al id
					id = id + games[key].players[playerKey].socket.id;
				}
				games[key].id = id;
				opponentFound = true;
			}
		};
		//si no encuentro juegos con un solo jugador creo un nuevo juego para el jugador
		if(!opponentFound){
			//creo al jugador
			var gamePlayers = [];
			var player1 = {
				id: players[email].id,
				status: 'connected',
				socket: socket,
				email: email,
				name: players[email].name,
				index: 0
			};
			gamePlayers.push(player1);
			//creo una partida
			var game = {
				turn: 0,
				players: gamePlayers,
			};
			//guardo el juego
			games.push(game);
			//guardo al jugador
			players[email].gameIndex = games.indexOf(game);
			players[email].playerIndex = 0;
		}
	});

	//cuando un jugador solicita el listado de personajes
	socket.on("GetCharacters", function(data){
		//obtengo el listado de personajes
		mysqlConnection.FindAllCharacters(function (result) {
			socket.emit("SetCharacters",result);
		});
	});

	//cuando un jugador me envia su seleccion de personajes
	socket.on("SendCharactersSelected", function(data){
		//seteo al jugador en modo de espera
		games[players[data.email].gameIndex].players[players[data.email].playerIndex].status = "waitingForConnection";
		//agrego el socket al jugador
		players[data.email].socket=socket;
		games[players[data.email].gameIndex].players[players[data.email].playerIndex].socket = socket;
		//guardo la seleccion dentro del personaje dentro del juego
		games[players[data.email].gameIndex].players[players[data.email].playerIndex].charactersIndex = data.selection;
		//verifrico si ya ambos jugadores estan listos para jugar
		var readyToGame = true;
		for (var playerKey in games[players[data.email].gameIndex].players){
			if(games[players[data.email].gameIndex].players[playerKey].status!="waitingForConnection"){
				readyToGame=false;
			}
		}
		//si amnbos jugadores estan listos para empezar
		if(readyToGame){
			//verifico que existan servidores conectados
			if(Object.keys(modularServers).length){
				//si los hay busco el servidor con menor cantidad de juegos
				var selectedServerKey = "";
				for(var serverKey in modularServers){
					if(selectedServerKey==""||modularServers[selectedServerKey].games.length>modularServers[serverKey].games.length){
						selectedServerKey = serverKey;
					}
				}
				//genero un clon del juego
				var gameClone = Object.assign({} , games[players[data.email].gameIndex]);
				//clono los jugadores
				gameClone.players = Object.assign({} , gameClone.players);
				//elimino los socket del juego clonado (no se pueden enviar)
				for (var playerKey in gameClone.players){
					//primero clono el jugador
					gameClone.players[playerKey] = Object.assign({} , gameClone.players[playerKey]);
					//elimino el socket
					gameClone.players[playerKey].socket = "";
				}

				//envio el juego al servidor encontrado
				modularServers[serverKey].socket.emit("SendGame",gameClone);
				//guardo la key del servidor en el juego por si quiero reconectarme mas adelante
				games[players[data.email].gameIndex].serverKey = serverKey;

				//informo a ambos jugadores que ya tienen un servidor asignado
				for (var playerKey in games[players[data.email].gameIndex].players){
					games[players[data.email].gameIndex].players[playerKey].socket.emit("SetServer", modularServers[serverKey].address);
					//cambio el estado de los jugadores por si necesitan reconectarse mas adelante
					games[players[data.email].gameIndex].players[playerKey].status = "onGame";
				}
			}
		}
	});

	//cuando un servidor modular me indica que ha terminado un juego
	socket.on("EndGame", function(data){
		//verifico que el servidor exista el servidor que esta informando el termino de juego
		if(modularServers[socket.id]){
			//procedo a eliminar el juego
			var foundIndex=""
			for(var gameIndex in games){
				if(games[gameIndex].id==data){
					foundIndex=gameIndex;
				}
			}
			//busco los jugadores a eliminar
			for (var playerKey in games[foundIndex].players){
				//elimino el jugador
				delete players[games[foundIndex].players[playerKey].email];
			}
			//elimino el juego de mi lista
			games.splice(foundIndex, 1);
		}
	});

	//cuando un servidor modular me indica que no encuentra un jugador
	socket.on("PlayerNotFound", function(email){
		//verifico que el servidor exista el servidor que esta informando el termino de juego
		if(modularServers[socket.id]&&players[email]){
			var gameIndex = players[email].gameIndex;
			//busco los personajes a eliminar
			if(games[gameIndex]){
				for (var playerKey in games[gameIndex].players){
					//elimino el jugador
					delete players[games[gameIndex].players[playerKey].email];
				}
				//elimino el juego de mi lista
				games.splice(gameIndex, 1);
			}
		}

	});

	//cuando un servidor modular me indica cuantas partidas esta administrando
	socket.on("reportGamesCount", function(data){
		//verifico que el servidor exista el servidor
		if(modularServers[socket.id]){
			modularServers[socket.id].games = data;
		}
	});
});
