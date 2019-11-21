//creo el servidor
var express=require("express");
var app=express();
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
var modularServers = [];

//aqui se guardaran las partidas en curso
var games = [];

//aqui se guardan los datos escenciales de los jugadores (para acceder rapidamente a sus partidas)
var players = [];


io.on("connection",function(socket){
	//cuando un servidor modular se conecta
	socket.on("SuscribeServer", function(address, callback){
		console.log(new Date(Date.now()).toLocaleString()+" Modular server connected from "+socket.request.connection.remoteAddress);
		//guardo el servidor modular en mi lista de servidores
		modularServers[socket.id] = [];
		modularServers[socket.id].socket = socket;
		modularServers[socket.id].address = address;
		modularServers[socket.id].games = 0;
		callback();
	});

	//cuando un jugador se conecta
	socket.on("SuscribeClient", function(data){
		//informo que se conecto un cliente
		console.log(new Date(Date.now()).toLocaleString()+" Client ("+data.email+") connected from "+socket.request.connection.remoteAddress);

		//guardo al jugador
		players[data.email] = [];
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
				//elimino los socket del juego clonado (no se peuden enviar)
				for (var playerKey in gameClone.players){
					//primero clono el jugador
					gameClone.players[playerKey] = Object.assign({} , gameClone.players[playerKey]);
					//elimino el socket
					gameClone.players[playerKey].socket = "";
				}

				//envio el juego al servidor encontrado
				modularServers[serverKey].socket.emit("SendGame",gameClone);
				//informo a ambos jugadores que ya tienen un servidor asignado
				for (var playerKey in games[players[data.email].gameIndex].players){
					games[players[data.email].gameIndex].players[playerKey].socket.emit("SetServer", modularServers[serverKey].address);
				}

			}
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

				//informo a los jugadores que ya se encontro su oponente
				for(var playerKey in games[key].players){
					games[key].players[playerKey].status="selectingChars";
					games[key].players[playerKey].socket.emit("OpponentFound",key);
				}
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
	})
});
