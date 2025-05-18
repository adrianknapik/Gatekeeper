package handlers

import (
	"encoding/json"
	"net/http"
	"strconv"
	"sync"

	"github.com/gorilla/mux"
	"go-publisher-demo/models"
)

var (
	publishers = []models.Publisher{
		{ID: 1, Name: "O'Reilly Media", Country: "USA"},
		{ID: 2, Name: "Addison-Wesley", Country: "USA"},
		{ID: 3, Name: "Manning Publications", Country: "USA"},
		{ID: 4, Name: "Packt Publishing", Country: "UK"},
		{ID: 5, Name: "Apress", Country: "USA"},
		{ID: 6, Name: "No Starch Press", Country: "USA"},
		{ID: 7, Name: "Pearson", Country: "USA"},
		{ID: 8, Name: "Springer", Country: "Germany"},
		{ID: 9, Name: "Cambridge University Press", Country: "UK"},
		{ID: 10, Name: "Typotex Kiadó", Country: "Hungary"},
	}
	nextID     = 11
	mu         sync.Mutex
)

func GetAllPublishers(w http.ResponseWriter, r *http.Request) {
	mu.Lock()
	defer mu.Unlock()
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(publishers)
}

func GetPublisherByID(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid publisher ID", http.StatusBadRequest)
		return
	}

	mu.Lock()
	defer mu.Unlock()
	for _, p := range publishers {
		if p.ID == id {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(p)
			return
		}
	}

	http.Error(w, "Publisher not found", http.StatusNotFound)
}

func CreatePublisher(w http.ResponseWriter, r *http.Request) {
	var p models.Publisher
	if err := json.NewDecoder(r.Body).Decode(&p); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	mu.Lock()
	p.ID = nextID
	nextID++
	publishers = append(publishers, p)
	mu.Unlock()

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusCreated)
	json.NewEncoder(w).Encode(p)
}

func UpdatePublisher(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid publisher ID", http.StatusBadRequest)
		return
	}

	var updated models.Publisher
	if err := json.NewDecoder(r.Body).Decode(&updated); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	mu.Lock()
	defer mu.Unlock()
	for i, p := range publishers {
		if p.ID == id {
			updated.ID = id
			publishers[i] = updated
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(updated)
			return
		}
	}

	http.Error(w, "Publisher not found", http.StatusNotFound)
}

func DeletePublisher(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid publisher ID", http.StatusBadRequest)
		return
	}

	mu.Lock()
	defer mu.Unlock()
	for i, p := range publishers {
		if p.ID == id {
			publishers = append(publishers[:i], publishers[i+1:]...)
			w.WriteHeader(http.StatusNoContent)
			return
		}
	}

	http.Error(w, "Publisher not found", http.StatusNotFound)
}
