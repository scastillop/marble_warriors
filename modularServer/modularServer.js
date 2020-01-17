//creo el servidor
var express=require("express");
var app=express();

//genero variables para la visualizacion de datos en navegador
var debugPort = 8090;
app.get('/', function (req, res) {
	//hago un resumen d elos datos que quiero mostrar
	total = {};
	total.characters = characters;
	total.players = players;
	total.games = games;
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

//genero las variables de servidor
var server=require("http").createServer(app);
var ioServer=require("socket.io").listen(server);
var ioClient=require('socket.io-client');
var serverUrl='http://fex02.ddns.net';//'http://fex02.ddns.net';
var serverPort=9000;
var myUrl='http://fex02.ddns.net';//'http://fex02.ddns.net';
var myPort=9010;

//aqui se guardan los datos de los personajes
var characters = [];

//aqui se guardaran las partidas en curso
var games = [];

//aqui se guardan los datos escenciales de los jugadores (para acceder rapidamente a sus partidas)
var players = {};

// instancio conexion con la base de datos
var mysqlConnection = require('../databaseConnection/mysqlConnector');

//rescato los datos de los personajes desde la db
console.log(new Date(Date.now()).toLocaleString()+' Getting characters from data base...');
mysqlConnection.FindAllCharactersWithDetails(function (result) {
	if(result){
		console.log(new Date(Date.now()).toLocaleString()+' Characters obtained!');
		//guardo los personajes
		characters=result;

		//inicio el cliente
		socketClient = ioClient.connect(serverUrl+":"+serverPort);

		//si me logro connectar
		socketClient.on('connect', () => {
			//envio mis datos
			socketClient.emit('SuscribeServer', myUrl+":"+myPort, function(){
				//informo que se conecto
				console.log(new Date(Date.now()).toLocaleString()+' Connection with main server successful!'); 
			});
		});

		//si no logro conectarme 
		socketClient.on('connect_error', () => {
			//informo que no se conecto
			console.log(new Date(Date.now()).toLocaleString()+' Connection with main server failed'); // false
		});

		//si me desconecto del servidor
		socketClient.on('disconnect', () => {
			//informo
			console.log(new Date(Date.now()).toLocaleString()+' Connection with main server was lost'); // false
		});

		//cuando el servidor principal me envia un juego
		socketClient.on('SendGame', (data) => {
			games.push(data);
			ReportGamesCount();
		});

		//inicio el servidor
		server.listen(myPort, function() {
			console.log(new Date(Date.now()).toLocaleString()+' Server started on port '+myPort+'...');
		});
	}
});

//para efectos de prueba creo una partida (en el futuro esta debera ser creada por el servidor pincipal)
//creo los personajes de cada jugador
/*
var charactersP1 = [];
var charactersP2 = [];
for (var i = 0; i < 5; i++){
    charactersP1[i] = MakeChar(i);
    charactersP2[i] = MakeChar(i);
};

//creo los jugadores
var gamePlayers = [];
var player1 = {
	id: 1,
	name: "jugador 1",
	characters: charactersP1,
	status: 'waitingForConnection'
};
var player2 = {
	id: 2,
	name: "jugador 2",
	characters: charactersP2,
	status: 'waitingForConnection'
};
gamePlayers.push(player1);
gamePlayers.push(player2);

//creo una partida
var game = {
	turn: 0,
	turnBeginAt: Date.now(),
	players: gamePlayers,
};

//agrego la partida a la lista de partidas en curso
var gamesIndex = 1;
games[gamesIndex]=game;
gamesIndex++;
*/

//cuando un cliente se conecta
ioServer.on("connection",function(socket){

	//cuando un cliente me informa que esta listo para iniciar la partida
	socket.on("ReadyToBegin", function(email){
		var found = false;
		//informo que se conecto un cliente
		console.log(new Date(Date.now()).toLocaleString()+" Client ("+email+") connected from "+socket.request.connection.remoteAddress);
		//busco si este jugador ya esta guardado
		var playerFound=""
		for(var playerKey in players){
			if(players[playerKey].email==email){
				playerFound=playerKey;
			}
		}
		//si lo encontre
		if(playerFound!=""){
			//elimino el jugador antiguo
			delete players[playerFound];
		}
		//busco si existe algun juego que esté esperando este jugador
		for(var key in games){
			//busco juegos en los que esté este jugador
			for(var playerKey in games[key].players){
				//si lo encuentro 
				if(games[key].players[playerKey].email==email){
					found = true;
					//verifico si estaba conectado antes
					if(games[key].players[playerKey].status=="waitingForConnection"){
						//si el jugador no estaba agrego su socket en el juego y cambio el estado del jugador
						games[key].players[playerKey].socket = socket;
						games[key].players[playerKey].status = "connected";
						//asocio los datos de la partida y del juegador a su socket, para luego acceder rapidamente a ellos
						players[socket.id] = {};
						players[socket.id].gameIndex = key;
						players[socket.id].playerIndex = playerKey;
						players[socket.id].email = email;

						//obtengo los personajes para el jugador
						games[key].players[playerKey].characters = [];
						//recorro los personajes que tengo en el servidor
						for (var characterKey in characters){
							//recorro los personajes que tiene seleccionado el jugador
							for(var characterIndex in games[key].players[playerKey].charactersIndex){
								if(characters[characterKey].index==games[key].players[playerKey].charactersIndex[characterIndex]){
									character = JSON.parse(JSON.stringify(characters[characterKey]));
									character.expirables = [];
									games[key].players[playerKey].characters.push(character);
								}
							}
						}
						//verifico si ya ambos jugadores estan conectados
						var playersConnected = 0;
						for(var playerKey2 in games[key].players){
							if(games[key].players[playerKey2].status=="connected"){
								playersConnected ++;
							}
						}

						if(playersConnected==2){
							//si lo esta inicio el primer turno
							games[key].turn==1;
							//informo a los jugadores de que el juego ha empezado y actualizo el estado de los jugadores
							for(var playerKey in games[key].players){
								//cambio el estado del jugador
								games[key].players[playerKey].status="selectingActions";
								//busco al enemigo
								for(var playerEnemyKey in games[key].players){
									//si el indice no es el mismo quiere decir que es el rival
									if(playerEnemyKey!=playerKey){
										//genero una variable donde estara la respuesta
										var allCharacters = [];
										//obtengo los personajes aliados
										allCharacters.push(games[key].players[playerKey].characters);
										//obtengo los personajes del enemigo
										allCharacters.push(games[key].players[playerEnemyKey].characters);
										//envio la respuesta al jugador
										games[key].players[playerKey].socket.emit("GameBegin", allCharacters);
									}
								}
							}
						}
					}else{
						//si el jugador ya estaba elimino el socket antiguo
						if(players[games[key].players[playerKey].socket]){
							delete players[games[key].players[playerKey].socket];	
						}
						//guardo su nuevo socket
						games[key].players[playerKey].socket = socket;
						//asocio los datos de la partida y del juegador a su socket, para luego acceder rapidamente a ellos
						players[socket.id] = {};
						players[socket.id].gameIndex = key;
						players[socket.id].playerIndex = playerKey;
						players[socket.id].email = email;
						//le envio el estado de los personajes
						GetAllCharacters(socket, "GameBegin");
					}
				}
			}
		};
		//si no encontre al jugador
		if(!found){
			//le informo al servidor principal para que no me lo mande de nuevo
			socketClient.emit('PlayerNotFound', email);
			//devuelvo al jugador al intro
			socket.emit("BackToIntro", "");
		}
	});

	//recibe las acciones enviadas por un cliente
	socket.on("Actions",function(actions){
		//guardo las acciones
		games[players[socket.id].gameIndex].players[players[socket.id].playerIndex].actions=actions;
		//actualizo el estado del jugador
		games[players[socket.id].gameIndex].players[players[socket.id].playerIndex].status="onBattlePhase";
		//verifico si ambos jugadores ya enviaron sus acciones
		var playersOnBattle = 0;
		for(var playerKey in games[players[socket.id].gameIndex].players){
			if(games[players[socket.id].gameIndex].players[playerKey].status=="onBattlePhase"){
				playersOnBattle ++;
			}
		}
		//si ambos estan en fase de batalla
		if(playersOnBattle==2){
			//actualizo los estados de los personajes
			UpdateTurnChanges(games[players[socket.id].gameIndex]);
			for(var playerKey in games[players[socket.id].gameIndex].players){
				//mostrar status de los jugadores
				/*
				for(var characterKey in games[players[socket.id].gameIndex].players[playerKey].characters){
					console.log(games[players[socket.id].gameIndex].players[playerKey].characters[characterKey].actualStat);	
				}
				*/
				//seteo la respuesta de las acciones (posteriormente esto se enviara a cada juagador)
				games[players[socket.id].gameIndex].players[playerKey].actionsResponse = [];
			}
			//si no se ha definido de quien es el turno de realizar una accion
			if(!games[players[socket.id].gameIndex].actionTurn){
				//seteo un jugador al azar
				games[players[socket.id].gameIndex].actionTurn = Object.keys(games[players[socket.id].gameIndex].players)[Math.floor(Math.random()*Object.keys(games[players[socket.id].gameIndex].players).length)];
			}

			//efectuo las acciones
			var pair=true;
			for (var i =0 ; i < 5; i++) {
				//defino al jugador en turno
				var playerOnTurn = games[players[socket.id].gameIndex].actionTurn;
				//defino al rival
				for(var playerKey in games[players[socket.id].gameIndex].players){
					if(playerKey!=games[players[socket.id].gameIndex].actionTurn){
						var playerRival = playerKey;
					}
				}
				//guardo el indice del juego para poder trabajar mas comodamente
				var gameIndex = players[socket.id].gameIndex;
				//si exiten acciones
				if(games[gameIndex].players[playerOnTurn].actions[i]){
					//guardo la accion para utilizarla mas adelante
					var action = games[gameIndex].players[playerOnTurn].actions[i];
					//reduzco el mana del que realizo la accion
					games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat.mp=games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat.mp-games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].cost;
					//guardo los datos de la habilidad (para ser usados en el cliente)
					action.name = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].name;
					action.animation = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].animation;
					action.distance = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].distance;
					action.target = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].target;
					action.effectIndex = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].effectIndex;
					action.projectileIndex = games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].projectileIndex;

					//seteo el o los objetivos
					var targets = []
					//si el objetivo es el propio personaje que usa la habilidad
					if(action.target=="own"){
						targets.push(action.owner);
					//si el objetivo es el team completo de quien usa la habilidad
					}else if(action.target=="ownTeam"){
						targets.push(0);
						targets.push(1);
						targets.push(2);
						targets.push(3);
						targets.push(4);
					//si el objetivo es un personaje unico y a eleccion
					}else if(action.target=="single"){
						targets.push(action.affected);
					//si el objetivo es un equipo completo a eleccion
					}else if(action.target=="team"){
						//si el afectado es mayor o igual a 5 es del equipo enemigo
						if(action.affected>=5){
							targets.push(5);
							targets.push(6);
							targets.push(7);
							targets.push(8);
							targets.push(9);
						//si no es el equipo aliado
						}else{
							targets.push(0);
							targets.push(1);
							targets.push(2);
							targets.push(3);
							targets.push(4);
						}
					//si el afectado es un grupo maximo de 3
					}else if(action.target=="multi3"){
						//agrego al propio afectado
						targets.push(action.affected);
						//si el afectado es mayor o igual a 5 es del equipo enemigo
						if(action.affected>=5){
							//agrego a sus compañeros
							if(action.affected-1>=5){
								targets.push(action.affected-1);
							}
							if(action.affected+1<=9){
								targets.push(action.affected+1);
							}
						//si no es el equipo aliado
						}else{
							//agrego a sus compañeros
							if(action.affected-1>=0){
								targets.push(action.affected-1);
							}
							if(action.affected+1<=4){
								targets.push(action.affected+1);
							}
						}
					}
					//verifico si se puede realizar la accion
					var canDo = false;
					if(action.affected>=5){
						if(CanDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerRival].characters[action.affected-5])){
							canDo = true;
						}
					}else{
						if(CanDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerOnTurn].characters[action.affected])){
							canDo = true;
						}
					}
					//si es que se puede realizar la accion
					if(canDo){
						//genero la accion de respuesta (datos que van a los jugadores)
						var actionResponse = action;
						//genero las variables para los objetivos
						var finalTargets =  [];
						var rivalTargets =  [];
						//recorro los objetivos
						for(var targetKey in targets){
							//si el afectado es del equipo enemigo
							if(targets[targetKey]>=5){
								//verifico si cumple con los requisitos
								if(CanDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerRival].characters[targets[targetKey]-5])){
									//si cumple las condiciones realizo los cambios en el afectado
									games[gameIndex].players[playerRival].characters[targets[targetKey]-5].actualStat = ChangeStat(games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat, games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerRival].characters[targets[targetKey]-5].actualStat, games[players[socket.id].gameIndex].players[playerRival].characters[games[players[socket.id].gameIndex].players[playerOnTurn].actions[i].affected-5].initialStat, games[gameIndex].turn, games[gameIndex].players[playerRival].characters[targets[targetKey]-5]);
										
									//guardo el nuevo objetivo
									var target = {};
									//primero le seteo los nuevos stats del afectado
									target.affectedStat = JSON.parse(JSON.stringify(games[gameIndex].players[playerRival].characters[targets[targetKey]-5].actualStat));
									target.affected = targets[targetKey];
									finalTargets.push(target);

									//ahora para el rival
									var rivalTarget = JSON.parse(JSON.stringify(target));
									rivalTarget.affected = targets[targetKey]-5;
									rivalTargets.push(rivalTarget);

								}	
							}else{
								//si el afectado es del mismo equipo
								//verifico si cumple con los requisitos
								if(CanDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerOnTurn].characters[targets[targetKey]])){
									//si cumple las condiciones realizo los cambios en el afectado
									games[gameIndex].players[playerOnTurn].characters[targets[targetKey]].actualStat = ChangeStat(games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat, games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerOnTurn].characters[targets[targetKey]].actualStat, games[gameIndex].players[playerOnTurn].characters[targets[targetKey]].initialStat, games[gameIndex].turn, games[gameIndex].players[playerOnTurn].characters[targets[targetKey]]);

									//guardo el nuevo objetivo
									var target = {};
									//primero le seteo los nuevos stats del afectado
									target.affectedStat = JSON.parse(JSON.stringify(games[gameIndex].players[playerOnTurn].characters[targets[targetKey]].actualStat));
									target.affected = targets[targetKey];
									finalTargets.push(target);

									//ahora para el rival
									var rivalTarget = JSON.parse(JSON.stringify(target));
									rivalTarget.affected = targets[targetKey]+5;
									rivalTargets.push(rivalTarget);
								}
							}
						}
						//seteo el estado del owner
						actionResponse.ownerStat = Object.assign({} , games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat);
						//seteo los objetivos
						actionResponse.targets = finalTargets;
						//guardo la accion en la respuesta para el jugador en turno
						games[gameIndex].players[playerOnTurn].actionsResponse.push(actionResponse);
						if(action.affected>=5){
							//actualizo los ids de los personajes pensando en el rival
							var rivalResponse = Object.assign({} , actionResponse);
							rivalResponse.owner = actionResponse.owner+5;
							rivalResponse.affected = actionResponse.affected-5;
							rivalResponse.targets = rivalTargets;
							//guardo la accion en la respuesta para el rival
							games[gameIndex].players[playerRival].actionsResponse.push(rivalResponse);
						}else{
							//actualizo los ids de los personajes pensando en el rival
							var rivalResponse = Object.assign({} , actionResponse);
							rivalResponse.owner = actionResponse.owner+5;
							rivalResponse.affected = actionResponse.affected+5;
							rivalResponse.targets = rivalTargets;
							//guardo la accion en la respuesta para el rival
							games[gameIndex].players[playerRival].actionsResponse.push(rivalResponse);
						}
					}

					
				}
				//cambio al jugador que ahora tiene el turno de realizar una accion
				games[players[socket.id].gameIndex].actionTurn = playerRival;
				if(pair){
					i--;
				}
				pair=!pair;
			}
			//cambio el turno
			games[players[socket.id].gameIndex].turn++;
			//envío la respuesta a ambos jugadores
			for(var playerKey in games[players[socket.id].gameIndex].players){
				games[players[socket.id].gameIndex].players[playerKey].socket.emit("ActionsResponse", games[players[socket.id].gameIndex].players[playerKey].actionsResponse);
				//seteo al jugador nuevamente en estado de seleccion de acciones
				games[players[socket.id].gameIndex].players[playerKey].status="selectingActions";
			}

			//ver por consola el estado de los jugadores
			/*
			for(var playerKey in games[players[socket.id].gameIndex].players){
				for(var characterKey in games[players[socket.id].gameIndex].players[playerKey].characters){
					console.log(games[players[socket.id].gameIndex].players[playerKey].characters[characterKey].actualStat);	
				}
			}
			*/
		}
		//console.log(variable);
		//console.log("se han recibido las acciones seleccionadas por un jugador");
	});

	//si un cliente se desconecta
	socket.on('Surrender',function(){
		//compruebo que el jugador este en mi lista de jugadores
		if(players[socket.id]){
			//informo al servidor principal que el juego ha terminado
			socketClient.emit('EndGame', games[players[socket.id].gameIndex].id);
			//informo a los jugadores quien perdio y quien gano
			for(var playerKey in games[players[socket.id].gameIndex].players){
				if(games[players[socket.id].gameIndex].players[playerKey].socket.id!=socket.id){
					var winner = games[players[socket.id].gameIndex].players[playerKey];
					games[players[socket.id].gameIndex].players[playerKey].socket.emit("Victory", "(by surrender)");
				}else{
					var loser = games[players[socket.id].gameIndex].players[playerKey];
					games[players[socket.id].gameIndex].players[playerKey].socket.emit("Defeat", "(by surrender)");
				}
			}
			//guardo los resultados en db
			mysqlConnection.SaveGameResults(winner, loser);
			//elimino el juego de mi lista
			games.splice(players[socket.id].gameIndex, 1);
			//informo la cantidad de juegos que administro
			ReportGamesCount();
		}
	});

	//funcion que actualiza el estado de los personajes para los clientes
	socket.on('UpdateCharacters',function(){
		//verifico que el juego exista
		if(games[players[socket.id].gameIndex]){
			UpdateTurnChanges(games[players[socket.id].gameIndex]);
			GetAllCharacters(socket, "CharactersUpdate");	
		}
	});
	//si un cliente se desconecta
	/*
	socket.on('disconnect',function(){
		console.log(new Date(Date.now()).toLocaleString()+" Client disconnected from "+socket.request.connection.remoteAddress+", with id:"+socket.id);
		//compruebo que el jugador este en mi lista de jugadores
		if(players[socket.id]){
			//si esta informo al oponente que ha ganado por leave
			for(var playerKey in games[players[socket.id].gameIndex].players){
				//mientras no sea el mismo jugador que me evia el mensaje y este conectado
				if(games[players[socket.id].gameIndex].players[playerKey].status=="connected"){
					if(games[players[socket.id].gameIndex].players[playerKey].socket.id!=socket.id){
						games[players[socket.id].gameIndex].players[playerKey].socket.emit("VictoryByLeave", players[socket.id].gameIndex);
					}
				}
			}
		}
	});
	*/
})


function MakeChar(position){
	//genero estadisticas para una habilidad
	var statSkill = {
		hp: -40,
	    mp: 0,
	    atk: 0,
	    def: 0,
	    spd: 0,
	    mst: 0,
	    mdf: 0
	};

	//genero una habilidad
	var skill = {
		id: 1,
		skillName: "Basic Attack",
	    cost: 10,
	    stats: statSkill,
	    type: "physical"
	};

	//guardo la habilidad en una lista
	skillSet = [];
	skillSet[0] = skill;

	//genero las estadisticas del personaje
	var statChar = {
		hp: 100,
	    mp: 100,
	    atk: 30,
	    def: 20,
	    spd: 30,
	    mst: 30,
	    mdf: 20
	};

	//genero el personaje
	var character = {
		id: 1,
	    characterName: "Swordman",
	    position: position,
	    initialStat: statChar,
	    actualStat: statChar,
	    skills: skillSet
	}

	//entrego el personaje
	return character;
}

//funcion que calcula el efecto de los stats de habilidades sobre los stat de personajes
function ChangeStat(statOwner, skill, statAffected, initialStat, turn, affected){
	//recorro los atributos del stat de la habilidad
	var statSkill = skill.stats;
	for(var attributeSkill in statSkill){
		//valido que el cambio sea mayor que cero y no se trate del id
		if(statSkill[attributeSkill]!=0&&attributeSkill!="id"){
			//si es una modificacion de hp 
			if(attributeSkill=="hp"){
				//debo averiguar si es ataque o recuperacion
				if(statAffected[attributeSkill]+statSkill[attributeSkill]<statAffected[attributeSkill]){
					//es un ataque! debo ver que tipo de defensa tengo que aplicar
					switch(skill.type){
						case "physical":
							//calculo cuanto aumentara el daño con el atk del owner
							var damage = statSkill.hp + statSkill.hp*(statOwner.atk/100);
							//calculo cuanto se reducira el daño con la defensa fisica del afectado 
							var damage = damage - (damage*(statAffected.def/100));
						break;
						case "magical":
							//calculo cuanto aumentara el daño con el mst del owner
							var damage = statSkill.hp + statSkill.hp*(statOwner.mst/100);
							//calculo cuanto se reducira el daño con la defensa magica del afectado 
							var damage = damage - (damage*(statAffected.mdf/100));
						break;
					}
					//si al afectar el atributo queda en negativo debo dejarlo solo en 0
					if(statAffected[attributeSkill]+damage<0){
						statAffected[attributeSkill] = 0;
					}else{
						//si no, solo ejecuto el daño
						statAffected[attributeSkill] = statAffected[attributeSkill]+damage;
					}
				}else{
					//es recuperacion! valido que no me recupere mas del maximo
					if(statAffected[attributeSkill]+statSkill[attributeSkill]>initialStat[attributeSkill]){
						statAffected[attributeSkill] = initialStat[attributeSkill];
					}else{
						//de lo contrario solo efecuto la recuperacion
						statAffected[attributeSkill] = statAffected[attributeSkill]+statSkill[attributeSkill];
					}
				}
			}else if(attributeSkill=="mp"){
				//si es mp valido que no reduzca menos de 0
				if(statAffected[attributeSkill]+statSkill[attributeSkill]<0){
					statAffected[attributeSkill] = 0;
				}else{
					//de lo contrario valido que si es mp no exeda el maximo del personaje
					if(statAffected[attributeSkill]+statSkill[attributeSkill]>initialStat[attributeSkill]){
						statAffected[attributeSkill]=initialStat[attributeSkill];
					}else{
						//si no, solo aplico los cambios
						statAffected[attributeSkill]=statAffected[attributeSkill]+statSkill[attributeSkill];
					}
				}
			}else{
				//si no es ni hp ni mp valido si es un cambio con duracion
				if(skill.duration>0){
					//si tiene duracion				
					var difference = 0;
		 			//valido que no reduzca menos de 0
					if(statAffected[attributeSkill]+statSkill[attributeSkill]<0){
						//guardo la diferencia 
						difference = 0-JSON.parse(JSON.stringify(statAffected[attributeSkill]));
						//aplico el cambio
						statAffected[attributeSkill] = 0;
					}else{
						//si no, guardo la diferencia 
						difference = 0-JSON.parse(JSON.stringify(statSkill[attributeSkill]));
						//aplico los cambios
						statAffected[attributeSkill]=statAffected[attributeSkill]+statSkill[attributeSkill];
					}
					expirable = {};
					expirable.stat = attributeSkill;
					expirable.value = difference;
					expirable.calledAtTurn = turn;
					expirable.duration = skill.duration;
					affected.expirables.push(expirable);
				//si no tiene duracion
				}else{
					//valido que no reduzca menos de 0
					if(statAffected[attributeSkill]+statSkill[attributeSkill]<0){
						statAffected[attributeSkill] = 0;
					}else{
						//si no, solo aplico los cambios
						statAffected[attributeSkill]=statAffected[attributeSkill]+statSkill[attributeSkill];
					}
				}
			}
		}
	}
	return statAffected;
}

//funcion que indica si un personaje puede realizar una accion
function CanDo(owner, skill, affected){
	//el owner debe estar vivo
	//el owner debe tener mana suficiente
	//el affected debe estar vivo
	if(owner.actualStat.hp>0&&owner.actualStat.mp>=skill.cost&&affected.actualStat.hp>0){
		return true;
	}else{
		return false;
	}
}

//funcion que informa al servidor principal cuantas partidas estoy trabajando
function ReportGamesCount(){
	socketClient.emit('ReportGamesCount', games.length);	
}

//funcion que actualiza los cambios que duran por turnos
function UpdateTurnChanges(game){
	for(var playerKey in game.players){
		for(var characterKey in game.players[playerKey].characters){
			for(var expirableKey in game.players[playerKey].characters[characterKey].expirables){
				if(game.turn>=game.players[playerKey].characters[characterKey].expirables[expirableKey].calledAtTurn+game.players[playerKey].characters[characterKey].expirables[expirableKey].duration){
					game.players[playerKey].characters[characterKey].actualStat[game.players[playerKey].characters[characterKey].expirables[expirableKey].stat] = game.players[playerKey].characters[characterKey].actualStat[game.players[playerKey].characters[characterKey].expirables[expirableKey].stat] + game.players[playerKey].characters[characterKey].expirables[expirableKey].value;
					game.players[playerKey].characters[characterKey].expirables.splice(expirableKey,1);
				}
			}
		}
	}
}

//funcion que envia el estado actual de los personajess
function GetAllCharacters(socket, method){
	//verifico que el juego exista
	if(games[players[socket.id].gameIndex]){

		var endGame = false;
		var gameFound = true;
		//verifico que el juego aun no termina
		for (var playerKey in games[players[socket.id].gameIndex].players){
			if(games[players[socket.id].gameIndex]){
				var alldeath = true;
				for (var charKey in games[players[socket.id].gameIndex].players[playerKey].characters){
					if(games[players[socket.id].gameIndex].players[playerKey].characters[charKey].actualStat.hp>0){
						alldeath = false;
					}
				}
				//si todos los personajes del jugador estan muertos el juego termino
				if(alldeath){
					endGame = true;
					//informo al servidor prubcipal
					socketClient.emit('EndGame', games[players[socket.id].gameIndex].id);
					//informo a los jugadores quien perdio y quien gano
					for(var playerKey2 in games[players[socket.id].gameIndex].players){
						if(playerKey2!=playerKey){
							var winner = games[players[socket.id].gameIndex].players[playerKey2];
							games[players[socket.id].gameIndex].players[playerKey2].socket.emit("Victory", "");
						}else{
							var loser = games[players[socket.id].gameIndex].players[playerKey2];
							games[players[socket.id].gameIndex].players[playerKey2].socket.emit("Defeat", "");
						}
					}
					//guardo los resultados en db
					mysqlConnection.SaveGameResults(winner, loser);
					//elimino el juego de mi lista
					if(players[socket.id]){
						games.splice(players[socket.id].gameIndex, 1);
						//informo la cantidad de juegos que administro
						ReportGamesCount();	
					}
					
				}
			}else{
				gameFound = false;
			}
		}
		//si el juego existe y aun no termina
		if(!endGame&&gameFound){
			//identifico el juego
			key = players[socket.id].gameIndex;
			//recorro los jugadores del juego
			for(var playerKey in games[key].players){
				if(games[key].players[playerKey].email==players[socket.id].email){
					//busco al enemigo
					for(var playerEnemyKey in games[key].players){
						//si el indice no es el mismo quiere decir que es el rival
						if(playerEnemyKey!=playerKey){
							//genero una variable donde estara la respuesta
							var allCharacters = [];
							//obtengo los personajes aliados
							allCharacters.push(games[key].players[playerKey].characters);
							//obtengo los personajes del enemigo
							allCharacters.push(games[key].players[playerEnemyKey].characters);
							//envio la respuesta al jugador
							games[key].players[playerKey].socket.emit(method, allCharacters);
						}
					}
				}
			}
		}
	}
	
}