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
				return $http.post("/scs/alcontenttree.scsvc", data);
			},
			getAll: function() {
				return $http.get("/scs/algetall.scsvc");
			},
			getToday: function() {
				return $http.get("/scs/algettoday.scsvc");
			},
			query: function(value, field, types, start, end) {
				var data = { "filters": value, "field": field, "eventTypes": types, "start": start, "end": end };
				return $http.post("/scs/alqueryactivity.scsvc", data);
			},
			rebuildIndex: function() {
				return $http.get("/scs/alrebuildindex.scsvc");
			},
			eventTypeList: function() {
				return $http.get("/scs/aleventtypes.scsvc");
			},
			getUsers: function() {
				return $http.get("/scs/alusers.scsvc");
			},
			getAutoComplete: function(text, start, end, types) {
				var data = { "text": text, "start": start, "end": end, "eventTypes": types };
				return $http.post("/scs/alautocomplete.scsvc", data);
			},
			getData: function(value, field, types, start, end, page) {
				var data = { "filters": value, "field": field, "eventTypes": types, "start": start, "end": end, "page": page };
				return $http.post("/scs/alactivitydata.scsvc", data);
			},
			rebuild: function() {
				return $http.get("/scs/alrebuild.scsvc");
			},
			rebuildStatus: function() {
				return $http.get("/scs/alrebuildstatus.scsvc");
			}
	};

		return service;

		function getData() { }
	}
})();