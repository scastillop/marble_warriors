var mysql = require('mysql');

var con = mysql.createConnection({
  host: "localhost",
  user: "root",
  password: "1234567890qwerty",
  database: "mydb"
});

var getInformationFromDB = function(sql, dataWhere, callback) {
	con.connect(function(err) {
	  if (err) throw err;
	  con.query(sql, dataWhere, function (err, result, fields) {
	  	if (err) callback(err,null);
        else callback(null,result);
	  });
	});
}

exports.findAllCharacters = function (callback) {
	getInformationFromDB('SELECT * FROM characters', [], function (err, result) {
	  if (err) callback(err,null);
      else callback(null,result);
	});
};

exports.findUserByEmail = function (email, callback) {
  getInformationFromDB('SELECT * FROM users where email = ?', [email], function (err, result) {
	  if (err) callback(err,null);
      else callback(null,result);
	});
};

exports.findUserByToken = function (token, callback) {
  getInformationFromDB('SELECT * FROM users where token = ?', [token], function (err, result) {
	  if (err) callback(err,null);
      else callback(null,result);
	});
};
