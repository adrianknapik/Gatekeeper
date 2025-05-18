package handlers

import (
	"encoding/json"
	"net/http"
	"strconv"
	"sync"
	"log"

	"github.com/gorilla/mux"
	"go-publisher-demo/models"
)

var (
	books   = []models.Book{
		{ID: 1, Title: "Go in Action", Author: "William Kennedy", PublisherID: 1},
		{ID: 2, Title: "The Go Programming Language", Author: "Alan A. A. Donovan", PublisherID: 1},
		{ID: 3, Title: "Introducing Go", Author: "Caleb Doxsey", PublisherID: 2},
		{ID: 4, Title: "Go Web Programming", Author: "Sau Sheong Chang", PublisherID: 2},
		{ID: 5, Title: "Go Programming Blueprints", Author: "Mat Ryer", PublisherID: 3},
		{ID: 6, Title: "Learning Go", Author: "Jon Bodner", PublisherID: 3},
		{ID: 7, Title: "Concurrency in Go", Author: "Katherine Cox-Buday", PublisherID: 4},
		{ID: 8, Title: "Go Systems Programming", Author: "Mihalis Tsoukalos", PublisherID: 4},
		{ID: 9, Title: "Network Programming with Go", Author: "Adam Woodbeck", PublisherID: 5},
		{ID: 10, Title: "Go Design Patterns", Author: "Mario Castro Contreras", PublisherID: 5},
		{ID: 11, Title: "Mastering Go", Author: "Mihalis Tsoukalos", PublisherID: 6},
		{ID: 12, Title: "Hands-On High Performance with Go", Author: "Bob Strecansky", PublisherID: 6},
	}
	bookID  = 13
	bookMux sync.Mutex
)

func GetAllBooks(w http.ResponseWriter, r *http.Request) {
	bookMux.Lock()
	defer bookMux.Unlock()
	w.Header().Set("Content-Type", "application/json")
	json.NewEncoder(w).Encode(books)
}

func GetBookByID(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid book ID", http.StatusBadRequest)
		return
	}

	bookMux.Lock()
	defer bookMux.Unlock()
	for _, b := range books {
		if b.ID == id {
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(b)
			return
		}
	}

	http.Error(w, "Book not found", http.StatusNotFound)
}

func CreateBook(w http.ResponseWriter, r *http.Request) {
	var b models.Book
	
	if err := json.NewDecoder(r.Body).Decode(&b); err != nil {
		log.Println("Error decoding request body:", err)
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	bookMux.Lock()
	b.ID = bookID
	bookID++
	books = append(books, b)
	bookMux.Unlock()

	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusCreated)
	json.NewEncoder(w).Encode(b)
}

func UpdateBook(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid book ID", http.StatusBadRequest)
		return
	}

	var updated models.Book
	if err := json.NewDecoder(r.Body).Decode(&updated); err != nil {
		http.Error(w, "Invalid request payload", http.StatusBadRequest)
		return
	}

	bookMux.Lock()
	defer bookMux.Unlock()
	for i, b := range books {
		if b.ID == id {
			updated.ID = id
			books[i] = updated
			w.Header().Set("Content-Type", "application/json")
			json.NewEncoder(w).Encode(updated)
			return
		}
	}

	http.Error(w, "Book not found", http.StatusNotFound)
}

func DeleteBook(w http.ResponseWriter, r *http.Request) {
	vars := mux.Vars(r)
	id, err := strconv.Atoi(vars["id"])
	if err != nil {
		http.Error(w, "Invalid book ID", http.StatusBadRequest)
		return
	}

	bookMux.Lock()
	defer bookMux.Unlock()
	for i, b := range books {
		if b.ID == id {
			books = append(books[:i], books[i+1:]...)
			w.WriteHeader(http.StatusNoContent)
			return
		}
	}

	http.Error(w, "Book not found", http.StatusNotFound)
}
