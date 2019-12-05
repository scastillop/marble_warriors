var mysql = require('mysql');

var con;

function connect(){
	con = mysql.createConnection({
	  host: "170.239.84.52",
	  user: "mwDev",
	  password: "qweqwe",
	  database: "mwDev"
	});

	con.connect(function(err) { 
		if(err) { 
			console.log(new Date(Date.now()).toLocaleString()+' Error when connecting to db:', err);
			setTimeout(connect, 2000);
		}                                    
	});                                    
		                              
	con.on('error', function(err) {
		if(err.code === 'PROTOCOL_CONNECTION_LOST') { 
			connect();
		} else {
			console.log(new Date(Date.now()).toLocaleString()+' Error when connecting to db:', err);
			setTimeout(connect, 2000);
		}
	});
}

connect();

//funcion que ejecuta consultas a la base de datos
var GetInformationFromDB = function(sql, dataWhere, callback) {
	con.query(sql, dataWhere, function (err, result, fields) {
		if (err){
			console.log(new Date(Date.now()).toLocaleString()+" Database error!: "+err);
			setTimeout(GetInformationFromDB(sql, dataWhere, function (res){callback(res)}), 2000);

		} else {
			callback(result);
		}
	});
}

//funcion que trae todos los personajes
exports.FindAllCharacters = function (callback) {
	GetInformationFromDB('SELECT c.id, c.index, c.name, s.hp, s.mp, s.atk, s.def, s.spd, s.mst, s.mdf FROM characters c JOIN stats s ON c.stats_id  = s.id', [], function (result) {
    	callback(result);
	});
};

//funcion que trae un usuario por su email
exports.FindUserByEmail = function (email, callback) {
  	GetInformationFromDB('SELECT * FROM users where email = ?', [email], function (result) {
    	callback(result);
	});
};

exports.FindUserByToken = function (token, callback) {
	GetInformationFromDB('SELECT * FROM users where token = ?', [token], function (result) {
    	callback(result);
	});
};

//funcion que crea un usuario a aprtir de un email
exports.InsertUser = function (email, name, callback) {
	GetInformationFromDB('INSERT INTO users (email, name) VALUES (?,?)', [email, name], function (result) {
    	callback(result);
	});
};

//funcion que trae todos los personajes
exports.FindAllCharacters2 = function (callback) {
	GetInformationFromDB('SELECT * FROM characters', [], function (result) {
    	callback(result);
	});
};

//funcion que trae todos los personajes
exports.FindAllCharactersSimple = function (callback) {
	GetInformationFromDB('SELECT * FROM characters', [], function (result) {
    	callback(result);
	});
};

//funcion que trae todos los stats
exports.FindAllStats = function (callback) {
	GetInformationFromDB('SELECT * FROM stats', [], function (result) {
    	callback(result);
	});
};

//funcion que trae todas las skills
exports.FindAllSkills = function (callback) {
	GetInformationFromDB('SELECT * FROM skills', [], function (result) {
    	callback(result);
	});
};

exports.FindAllCharactersWithDetails = function (callback) {
	var masterThis = this;
	//obtengo los perosnajes
	masterThis.FindAllCharactersSimple(function(characters){
		//obtengo todos los stats
		masterThis.FindAllStats(function(stats){
			//obtengo todas las skills
			masterThis.FindAllSkills(function(skills){
				//recorro las skills para asociarlas con sus stats
				for (var skillIndex in skills){
					//recorro los stats
					for (var statIndex in stats){
						//si el stat corresponde la skill
						if(skills[skillIndex].stat_id == stats[statIndex].id){
							//asigno el stat a la skill
							skills[skillIndex].stats=stats[statIndex];
						}
					}
				}
				//recorro los personajes para asociarlos con sus stats y skills
				for (var characterIndex in characters){
					//recorro los stats
					for (var statIndex in stats){
						//si el stat corresponde al personaje
						if(characters[characterIndex].stats_id == stats[statIndex].id){
							//asigno el stat al personaje
							characters[characterIndex].initialStat=stats[statIndex];
							characters[characterIndex].actualStat=Object.assign({} , stats[statIndex]);
						}
					}
					//procedo a asociar las skills
					charSkills =[];
					//recorro las skills
					for (var skillIndex in skills){
						//si la skill corresponde al personaje
						if(characters[characterIndex].id == skills[skillIndex].characters_id){
							//agrego la skill
							charSkills.push(skills[skillIndex])
						}
					}
					//asocio las skills al personaje
					characters[characterIndex].skills = charSkills;
				}

				//retorno el resultado
				callback(characters);
			});
		});
	});
};