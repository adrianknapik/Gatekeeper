package models

type Book struct {
	ID          int    `json:"id"`
	Title       string `json:"title"`
	Author      string `json:"author"`
	PublisherID int    `json:"publisher_id"`
}