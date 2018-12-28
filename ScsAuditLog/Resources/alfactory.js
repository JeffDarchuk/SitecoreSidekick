(function () {
	'use strict';

	angular
        .module('app')
        .factory('ALFactory', ALFactory);

	ALFactory.$inject = ['$http'];

	function ALFactory($http) {
		var service = {
			contentTree: function(id, database, server) {
				var data = { "id": id, "database": database, "server": server };
				return $http.post("/scs/al/alcontenttree.scsvc", data);
			},
			getAll: function() {
				return $http.get("/scs/al/algetall.scsvc");
			},
			getToday: function() {
				return $http.get("/scs/al/algettoday.scsvc");
			},
			query: function(value, field, types, start, end, databases) {
				var data = { "filters": value, "field": field, "eventTypes": types, "start": start, "end": end, "databases": databases };
				return $http.post("/scs/al/alqueryactivity.scsvc", data);
			},
			rebuildIndex: function() {
				return $http.get("/scs/al/alrebuildindex.scsvc");
			},
			eventTypeList: function() {
				return $http.get("/scs/al/aleventtypes.scsvc");
			},
			getUsers: function() {
				return $http.get("/scs/al/alusers.scsvc");
			},
			getAutoComplete: function(text, start, end, types) {
				var data = { "text": text, "start": start, "end": end, "eventTypes": types };
				return $http.post("/scs/al/alautocomplete.scsvc", data);
			},
			getData: function(value, field, types, start, end, page, databases) {
				var data = { "filters": value, "field": field, "eventTypes": types, "start": start, "end": end, "page": page, "databases": databases};
				return $http.post("/scs/al/alactivitydata.scsvc", data);
			},
			rebuild: function() {
				return $http.get("/scs/al/alrebuild.scsvc");
			},
			rebuildStatus: function() {
				return $http.get("/scs/al/alrebuildstatus.scsvc");
			},
			getDatabases: function() {
				return $http.get("/scs/al/algetdatabases.scsvc");
			}
	};

		return service;

		function getData() { }
	}
})();