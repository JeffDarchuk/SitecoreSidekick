(function () {
	'use strict';

	angular
        .module('app')
        .controller('alcontroller', alcontroller);

	alcontroller.$inject = ['ALFactory'];
	var WIDTH = 750,
	HEIGHT = 400,
	MARGINS = {
		top: 20,
		right: 20,
		bottom: 20,
		left: 70
	}
	var xScale;
	var yScale;

	function alcontroller(ALfactory) {
		/* jshint validthis:true */
		var vm = this;
		vm.treeEvents = {
			'click': function(val) {
				vm.treeEvents.selected = val;
				if (vm.getDescendants) {
					vm.query(val.Id, "descendants");
				} else {
					vm.query(val.Id.replace(/[^\w-]/g, '').replace(/-/g, '*'), "id");
				}
				
			}
		};
		vm.start = new Date();
		vm.start.setDate(vm.start.getDate() - 10);
		vm.start = $.datepicker.formatDate("M d, yy", vm.start);
		vm.end = $.datepicker.formatDate("M d, yy", new Date());
		vm.queryText = "*";
		vm.page = 0;
		vm.pageNum = 1;
		vm.pagination = new Object();
		vm.rebuildNum = -1;
		vm.getDescendants = false;
		vm.databases = new Object();
		ALfactory.getUsers()
			.then(function(response) {
				vm.users = response.data;
			});
		ALfactory.eventTypeList()
			.then(function(response) {
				vm.eventList = response.data;
				for (var key in vm.eventList) {
					vm[key] = true;
				}
				ALfactory.getDatabases().then(function (response) {
					vm.databases = response.data;
					vm.query(vm.queryText, "content");
				});
			});
		vm.queryAutoComplete = function(event) {
			if (event.which === 13)
				vm.query(vm.queryText, "content");
			else if (vm.queryText.length > 2) {
				ALfactory.getAutoComplete(vm.queryText, vm.start, vm.end, vm.getFilters())
					.then(function(response) {
						if (Object.keys(response.data).length !== 0)
							vm.autoComplete = response.data;
						else
							vm.autoComplete = false;
					});
			}
		}
		vm.resetPanes = function(identifier) {
			if (typeof (vm.panes) === "undefined")
				vm.panes = new Object();

			var el = document.getElementById('note' + identifier);
			if (el) {
				if (el.offsetHeight > el.offsetTop)
					el.style.marginTop = "-" + el.offsetTop + "px";
				else
					el.style.marginTop = "-" + el.offsetHeight + "px";
			}
			vm.panes[identifier] = true;
			for (var key in vm.panes) {
				if (key !== identifier)
					vm.panes[key] = false;
			}
		}
		vm.mainClick = function($event) {
			var el = $event.target;
			if (typeof (el.attributes["ng-click"]) == "object" &&
				el.attributes["ng-click"].nodeValue.indexOf("vm.resetPanes") > -1)
				return;
			while (el && el.localName !== "aldirective") {
				if (typeof(el.className) == "string" && el.className.indexOf("alpane") > -1)
					return;
				el = el.parentNode;
				if (el === null)
					return;
			}
			vm.resetPanes();
		}
		vm.rebuild = function() {
			if (confirm("Are you sure you would like to rebuild the index?")) {
				ALfactory.rebuild()
					.then(function(response) {
						if (!response.data)
							alert("error has occurred rebuilding");
						else {
							vm.rebuildStatus();
						}
					});
			}
		}
		vm.rebuildStatus = function() {
			ALfactory.rebuildStatus()
				.then(function(response) {
					vm.rebuildNum = response.data;
					if (vm.rebuildNum > -1) {
						setTimeout(function() {
								vm.rebuildStatus();
							},
							500);
					}
				});
		}

	vm.query = function (text, field) {
			if (field)
				vm.field = field;
			else
				vm.field = "content";
			vm.lastQuery = text;
			if (vm.field === "content")
				vm.queryText = text;
			ALfactory.query(text.split(/[\s,]+/), field, vm.getFilters(), vm.start, vm.end, vm.databases).then(function (response) {
				buildGraph(response.data);
				for (var key in response.data.GraphEntries) {
					drawLine(response.data.GraphEntries[key]);
				}
				vm.events = response.data.LogEntries;
			});
		}
		vm.getFilters = function() {
			var arr = new Array();
			for (var key in vm.eventList)
				if (vm[key])
					arr.push(key);
			return arr;
		}
		vm.getData = function (page) {
			if (page || page === 0)
				vm.page = page;
			vm.pageNum = vm.page + 1;
			var arr = new Array();
			for (var key in vm.eventList)
				if (vm[key])
					arr.push(key);
			ALfactory.getData(vm.lastQuery.split(/[\s,]+/), vm.field, arr, vm.start, vm.end, vm.page, vm.databases).then(function (response) {
				vm.events = response.data.results;
				vm.totalResults = response.data.total;
				vm.resultsPerPage = response.data.perPage;
				buildPagination(vm);
			});
		}
		vm.selectAll = function(event) {
			for (var i = 0; i < 100; i++)
				vm[i] = event.target.checked;
			vm.query(vm.queryText);
		}
		$("#alStartDate").datepicker({ dateFormat: "M d, yy" });
		$("#alEndDate").datepicker({ dateFormat: "M d, yy" });
	}
	function buildPagination(vm) {
		vm.totalPages = Math.ceil(vm.totalResults / vm.resultsPerPage);
		vm.pagination.next = (vm.page + 1) < vm.totalPages;
		vm.pagination.back = vm.page > 0;
		vm.pagination.pages = new Array();
		var count = 0;
		var lower;
		for (lower = vm.page; count < 3 && lower >= 0; lower--) {
			count++;
			vm.pagination.pages.push(lower);
		}
		for (var i = vm.page + 1; count < 5 && i < vm.totalPages; i++) {
			count++;
			vm.pagination.pages.push(i);
		}
		for (; count < 5 && lower >= 0; lower--) {
			count++;
			vm.pagination.pages.push(lower);
		}
		vm.pagination.pages.sort(function (a, b) { return a - b; });
	}
	function drawLine(g) {
		var parseDate = d3.time.format("%Y-%m-%d").parse;
		var vis = d3.select("#visualisation");
		var lineGen = d3.svg.line()
		.x(function (d) {
			return xScale(parseDate(d.X));
		})
		.y(function (d) {
			return yScale(d.Y);
		})
		.interpolate("basis");
			vis.append('svg:path')
				.attr('d', lineGen(g.Coordinates))
				.attr('style', "stroke:"+g.Color)
				.attr('stroke-width', 2)
				.attr('fill', 'none');
	}
	function buildGraph(o) {
		var parseDate = d3.time.format("%Y-%m-%d").parse;
		xScale = d3.time.scale().range([MARGINS.left, WIDTH - MARGINS.right]).domain([parseDate(o.XMin), parseDate(o.XMax)]);
		yScale = d3.scale.linear().range([HEIGHT - MARGINS.top, MARGINS.bottom]).domain([0, o.YMax]);
		var vis = d3.select("#visualisation"),
			xAxis = d3.svg.axis()
			.scale(xScale),
			yAxis = d3.svg.axis()
			.scale(yScale)
			.orient("left");
		vis.selectAll("*").remove();
		vis.append("svg:g")
			.attr("class", "x axis")
			.attr("transform", "translate(0," + (HEIGHT - MARGINS.bottom) + ")")
			.attr('stroke-width', 1)
			.call(xAxis);
		vis.append("svg:g")
			.attr("class", "y axis")
			.attr("transform", "translate(" + (MARGINS.left) + ",0)")
			.attr('stroke-width', 1)
			.call(yAxis);
	}
})();
