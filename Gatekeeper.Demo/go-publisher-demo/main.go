// main.go
package main

import (
	"log"
	"net/http"

	"github.com/gorilla/mux"
	"go-publisher-demo/handlers"
)

func main() {
	r := mux.NewRouter()

	// Publisher endpoints
	r.HandleFunc("/api/publisher_demo/publishers", handlers.GetAllPublishers).Methods("GET")
	r.HandleFunc("/api/publisher_demo/publishers/{id}", handlers.GetPublisherByID).Methods("GET")
	r.HandleFunc("/api/publisher_demo/publishers", handlers.CreatePublisher).Methods("POST")
	r.HandleFunc("/api/publisher_demo/publishers/{id}", handlers.UpdatePublisher).Methods("PUT")
	r.HandleFunc("/api/publisher_demo/publishers/{id}", handlers.DeletePublisher).Methods("DELETE")

	// Book endpoints
	r.HandleFunc("/api/publisher_demo/books", handlers.GetAllBooks).Methods("GET")
	r.HandleFunc("/api/publisher_demo/books/{id}", handlers.GetBookByID).Methods("GET")
	r.HandleFunc("/api/publisher_demo/books", handlers.CreateBook).Methods("POST")
	r.HandleFunc("/api/publisher_demo/books/{id}", handlers.UpdateBook).Methods("PUT")
	r.HandleFunc("/api/publisher_demo/books/{id}", handlers.DeleteBook).Methods("DELETE")

	log.Println("Server listening on :8080")
	if err := http.ListenAndServe(":8080", r); err != nil {
		log.Fatalf("could not start server: %v", err)
	}
}
