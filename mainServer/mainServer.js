//creo el servidor
var express=require("express");
var app=express();
var server=require("http").createServer(app);
var io=require("socket.io").listen(server);
var port=9000;

//inicio el servidor
server.listen(port, function() {
	console.log(new Date(Date.now()).toLocaleString()+' Servidor iniciado en el puerto '+port+'...');
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
		console.log(new Date(Date.now()).toLocaleString()+" Servidor modular conectado, desde "+socket.request.connection.remoteAddress);
		//guardo el servidor modular en mi lista de servidores
		modularServers[socket.id] = [];
		modularServers[socket.id].socket = socket;
		modularServers[socket.id].address = address;
		callback();
	});

	//cuando un jugador busca oponente
	socket.on("SearchOpponent", function(playerId){
		//informo que se conecto un cliente
		console.log(new Date(Date.now()).toLocaleString()+" Cliente conectado, desde "+socket.request.connection.remoteAddress);
		//busco si existe algun juego que esté esperando un jugador
		var opponentFound = false;
		for(var key in games){
			//busco juegos que tengan un solo jugador
			if(games[key].players.length==1){
				//si lo tiene agrego al jugador
				var player2 = {
					id: playerId,
					status: 'connected',
					socket: socket
				};
				games[key].players.push(player2);
				//asocio los datos de la partida y del juegador a su socket, para luego acceder rapidamente a ellos
				players[socket.id] = [];
				players[socket.id].gameIndex = key;
				players[socket.id].playerIndex = 1;

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
				id: playerId,
				status: 'connected',
				socket: socket
			};
			gamePlayers.push(player1);
			//creo una partida
			var game = {
				turn: 0,
				players: gamePlayers,
			};
			games.push(game);
		}
	})
});
