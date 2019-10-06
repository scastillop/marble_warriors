//creo el servidor
var express=require("express");
var app=express();
var server=require("http").createServer(app);
var io=require("socket.io").listen(server);
var port=9010;

//inicio el servidor
server.listen(port, function() {
	console.log(new Date(Date.now()).toLocaleString()+' Servidor iniciado en el puerto '+port+'...');
});

//aqui se guardaran las partidas en curso
var games = [];

//aqui se guardan los datos escenciales de los jugadores (para acceder rapidamente a sus partidas)
var players = [];

//para efectos de prueba creo una partida (en el futuro esta debera ser creada por el servidor pincipal)
//creo los personajes de cada jugador
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


//cuando un cliente se conecta
io.on("connection",function(socket){
	//informo que se conecto un cliente
	console.log(new Date(Date.now()).toLocaleString()+" Cliente conectado, desde "+socket.request.connection.remoteAddress+", con el id:"+socket.id);

	//cuando un cliente me informa que esta listo para iniciar la partida
	socket.on("readyToBegin", function(playerId){
		//busco si existe algun juego que esté esperando este jugador
		for(var key in games){
			//busco juegos que aun no han comenzado
			if(games[key].turn==0){
				//busco juegos en los que esté este jugador
				for(var playerKey in games[key].players){
					//si lo encuentro
					if(games[key].players[playerKey].id==playerId){
						//agrego su socket en el juego y cambio el estado del jugador
						games[key].players[playerKey].socket = socket;
						games[key].players[playerKey].status = "connected";
						//asocio los datos de la partida y del juegador a su socket, para luego acceder rapidamente a ellos
						players[socket.id] = [];
						players[socket.id].gameIndex = key;
						players[socket.id].playerIndex = playerKey; 
					}
				}
				//verifico si ya ambos jugadores estan conectados
				var playersConnected = 0;
				for(var playerKey in games[key].players){
					if(games[key].players[playerKey].status=="connected"){
						playersConnected ++;
					}
				}

				if(playersConnected==2){
					//si lo esta inicio el primer turno
					games[key].turn==1;
					//informo a los jugadores de que el juego ha empezado y actualizo el estado de los jugadores
					for(var playerKey in games[key].players){
						games[key].players[playerKey].status="selectingActions";
						games[key].players[playerKey].socket.emit("gameBegin",key);
					}
				}
			}
		};
	});

	//socket.emit("ping","lindo mensaje del servidor");

	//recibe las acciones enviadas por un cliente
	socket.on("actions",function(actions){
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
					//redusco el mana del que realizo la accion
					games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat.mp=games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat.mp-games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill].cost;
					//si el afectado es del equipo enemigo
					if(action.affected>=5){
						//verifico si cumple con los requisitos
						if(canDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerRival].characters[action.affected-5])){
							//si cumple las condiciones realizo los cambios en el afectado
							games[gameIndex].players[playerRival].characters[action.affected-5].actualStat = changeStat(games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat, games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerRival].characters[action.affected-5].actualStat, games[players[socket.id].gameIndex].players[playerRival].characters[games[players[socket.id].gameIndex].players[playerOnTurn].actions[i].affected-5].initialStat);
							
							//genero la accion de respuesta (datos que van a los jugadores)
							var actionResponse = action;
							//primero le seteo los nuevos stats del afectado
							actionResponse.affectedStat = games[gameIndex].players[playerRival].characters[action.affected-5].actualStat;
							//tambien del owner
							actionResponse.ownerStat = games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat;
							//guardo la accion en la respuesta para el jugador en turno
							games[gameIndex].players[playerOnTurn].actionsResponse.push(actionResponse);
							//actualizo los ids de los personajes pensando en el rival
							actionResponse.owner = actionResponse.owner+5
							actionResponse.affected = actionResponse.affected-5
							//guardo la accion en la respuesta para el rival
							games[gameIndex].players[playerOnTurn].actionsResponse.push(actionResponse);
						}	
					}else{
						//si el afectado es del mismo equipo
						//verifico si cumple con los requisitos
						if(canDo(games[gameIndex].players[playerOnTurn].characters[action.owner], games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerOnTurn].characters[action.affected])){
							//si cumple las condiciones realizo los cambios en el afectado
							games[gameIndex].players[playerOnTurn].characters[action.affected].actualStat = changeStat(games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat, games[gameIndex].players[playerOnTurn].characters[action.owner].skills[action.skill], games[gameIndex].players[playerOnTurn].characters[actions.affected].actualStat, games[gameIndex].players[playerOnTurn].characters[action.affected].initialStat);

							//genero la accion de respuesta (datos que van a los jugadores)
							var actionResponse = action;
							//primero le seteo los nuevos stats del afectado
							actionResponse.affectedStat = games[gameIndex].players[playerOnTurn].characters[action.affected].actualStat
							//tambien del owner
							actionResponse.ownerStat = games[gameIndex].players[playerOnTurn].characters[action.owner].actualStat;
							//guardo la accion en la respuesta para el jugador en turno
							games[gameIndex].players[playerOnTurn].actionsResponse.push(actionResponse);
							//actualizo los ids de los personajes pensando en el rival
							actionResponse.owner = actionResponse.owner+5
							actionResponse.affected = actionResponse.affected+5
							//guardo la accion en la respuesta para el rival
							games[gameIndex].players[playerOnTurn].actionsResponse.push(actionResponse);
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
			//envío la respuesta a ambos jugadores
			for(var playerKey in games[players[socket.id].gameIndex].players){
				games[players[socket.id].gameIndex].players[playerKey].socket.emit("actionsResponse", games[players[socket.id].gameIndex].players[playerKey].actionsResponse);
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
	socket.on('disconnect',function(){
		console.log(new Date(Date.now()).toLocaleString()+" Cliente desconectado "+socket.request.connection.remoteAddress+", con el id:"+socket.id);
		//compruebo que el jugador este en mi lista de jugadores
		if(players[socket.id]){
			//si esta informo al oponente que ha ganado por leave
			for(var playerKey in games[players[socket.id].gameIndex].players){
				//mientras no sea el mismo jugador que me evia el mensaje y este conectado
				if(games[players[socket.id].gameIndex].players[playerKey].status=="connected"){
					if(games[players[socket.id].gameIndex].players[playerKey].socket.id!=socket.id){
						games[players[socket.id].gameIndex].players[playerKey].socket.emit("victoryByLeave", players[socket.id].gameIndex);
					}
				}
			}
		}
	});
})


function MakeChar(position){
	//genero estadisticas para una habilidad
	var statSkill = {
		hp: -10,
	    mp: 0,
	    atk: 0,
	    def: 0,
	    spd: 0,
	    mst: 0,
	    mdf: 0
	};

	//genero una habilidad
	var skill = {
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
function changeStat(statOwner, skill, statAffected, initialStat){
	//recorro los atributos del stat de la habilidad
	var statSkill = skill.stats;
	for(var attributeSkill in statSkill){
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
		}else{
			//si no es hp valido que no reduzca menos de 0
			if(statAffected[attributeSkill]+statSkill[attributeSkill]<0){
				statAffected[attributeSkill] = 0;
			}else{
				//de lo contrario valido que si es mp no exeda el maximo del personaje
				if(attributeSkill=="mp"&&statAffected[attributeSkill]+statSkill[attributeSkill]>initialStat[attributeSkill]){
					statAffected[attributeSkill]=initialStat[attributeSkill];
				}else{
				//si no, solo aplico los cambios
					statAffected[attributeSkill]=statAffected[attributeSkill]+statSkill[attributeSkill];
				}
			}
		}
	}
	return statAffected;
}

//funcion que indica si un personaje puede realizar una accion
function canDo(owner, skill, affected){
	//el owner debe estar vivo
	//el owner debe tener mana suficiente
	//el affected debe estar vivo
	if(owner.actualStat.hp>0&&owner.actualStat.mp>=skill.cost&&affected.actualStat.hp>0){
		return true;
	}else{
		return false;
	}
}